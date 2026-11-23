using System.Security.Cryptography;
using System.Text;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class UploadOFXCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<IOFXParserService> _ofxParserService = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IAccountKeyHasher> _accountKeyHasher = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly IConfiguration _configuration;

    public UploadOFXCommandHandlerTests()
    {
        _accountKeyHasher
            .Setup(h => h.ComputeKey(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns("chave-da-conta");
        _accountKeyHasher
            .Setup(h => h.Last4(It.IsAny<string>()))
            .Returns("1234");
        _accountRepository
            .Setup(r => r.GetOrCreateDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Account.CreateDefault(Guid.NewGuid()));

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:Topics:BankImportCreated"] = "finance.bankimport.created",
            })
            .Build();
    }

    private UploadOFXCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object,
            _ofxParserService.Object,
            _categoryRepository.Object,
            _accountRepository.Object,
            _accountKeyHasher.Object,
            _eventPublisher.Object,
            _configuration);

    [Fact]
    public async Task Handle_DeveCriarBankImport_QuandoOFXValido()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = new List<OFXTransaction>
        {
            new("FIT001", DateTime.UtcNow, -150.00m, "IFOOD*RESTAURANTE", "DEBIT"),
            new("FIT002", DateTime.UtcNow, -50.00m,  "UBER TRIP",         "DEBIT"),
        };

        var parseResult = new OFXParseResult(
            AccountId: "12345",
            BankId: "001",
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: DateTime.UtcNow,
            Transactions: transactions);

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(parseResult);

        _categoryRepository
            .Setup(r => r.GetAllDefaultAsync(default))
            .ReturnsAsync([]);

        var command = new UploadOFXCommand(
            UserId: userId,
            FileStream: new MemoryStream(),
            FileName: "extrato.ofx");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("extrato.ofx");
        result.TotalRecords.Should().Be(2);

        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.UserId == userId &&
                b.FileName == "extrato.ofx" &&
                b.TotalRecords == 2),
            default), Times.Once);

        _eventPublisher.Verify(p =>
            p.PublishAsync(
                "finance.bankimport.created",
                It.IsAny<object>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveCalcularHashCorretamente()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15);
        var amount = -150.00m;
        var description = "IFOOD*RESTAURANTE";
        var type = "DEBIT";

        var raw = $"{date:yyyy-MM-dd}|{amount}|{description.Trim().ToUpperInvariant()}|{type}";
        var expectedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

        var transactions = new List<OFXTransaction>
        {
            new("FIT001", date, amount, description, type),
        };

        var parseResult = new OFXParseResult("123", "001", date, date, transactions);

        _ofxParserService.Setup(s => s.Parse(It.IsAny<Stream>())).Returns(parseResult);
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().Hash == expectedHash),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveSugerirCategoria_QuandoDescricaoContemPalavraChave()
    {
        // Arrange
        var alimentacaoCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Alimentação",
            IsDefault = true,
            Type = TransactionType.Expense,
        };

        var transactions = new List<OFXTransaction>
        {
            new("FIT001", DateTime.UtcNow, -50.00m, "IFOOD*RESTAURANTE", "DEBIT"),
        };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", DateTime.UtcNow, DateTime.UtcNow, transactions));

        _categoryRepository
            .Setup(r => r.GetAllDefaultAsync(default))
            .ReturnsAsync([alimentacaoCategory]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().SuggestedCategoryId == alimentacaoCategory.Id),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDefinirTipoExpense_QuandoValorNegativo()
    {
        // Arrange
        var transactions = new List<OFXTransaction>
        {
            new("FIT001", DateTime.UtcNow, -100.00m, "COMPRA", "DEBIT"),
        };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", DateTime.UtcNow, DateTime.UtcNow, transactions));

        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().Type == TransactionType.Expense &&
                b.Transactions.First().Amount == 100.00m),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDefinirTipoIncome_QuandoValorPositivo()
    {
        // Arrange
        var transactions = new List<OFXTransaction>
        {
            new("FIT001", DateTime.UtcNow, 5000.00m, "SALARIO", "CREDIT"),
        };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", DateTime.UtcNow, DateTime.UtcNow, transactions));

        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().Type == TransactionType.Income),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDefinirTipoExpense_QuandoTRNTYPEEhDEBIT()
    {
        // Arrange
        var transactions = new List<OFXTransaction>
    {
        new("FIT001", DateTime.UtcNow, -100.00m, "COMPRA", "DEBIT"),
    };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", DateTime.UtcNow, DateTime.UtcNow, transactions));

        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — TRNTYPE=DEBIT deve resultar em Expense independentemente do valor
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().Type == TransactionType.Expense),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDefinirTipoIncome_QuandoTRNTYPEEhCREDIT()
    {
        // Arrange
        var transactions = new List<OFXTransaction>
    {
        new("FIT001", DateTime.UtcNow, 5000.00m, "SALARIO", "CREDIT"),
    };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", DateTime.UtcNow, DateTime.UtcNow, transactions));

        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — TRNTYPE=CREDIT deve resultar em Income independentemente do valor
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.First().Type == TransactionType.Income),
            default), Times.Once);
    }

    // ── Resolução da conta da importação ──────────────────────────────────

    private void SetupOfx(string accountId = "12345", string bankId = "001")
    {
        var parseResult = new OFXParseResult(
            AccountId: accountId,
            BankId: bankId,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: DateTime.UtcNow,
            Transactions: [new("FIT001", DateTime.UtcNow, -10m, "PADARIA", "DEBIT")]);

        _ofxParserService.Setup(s => s.Parse(It.IsAny<Stream>())).Returns(parseResult);
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);
    }

    private static Account NewAccount(Guid userId, string name = "Nubank", string? key = null, bool archived = false) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = name,
        AccountKeyHash = key,
        IsArchived = archived,
    };

    [Fact]
    public async Task Handle_DeveReconhecerContaPeloOfx_QuandoNenhumaContaEscolhida()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, key: "chave-da-conta");
        SetupOfx();
        _accountRepository
            .Setup(r => r.GetByKeyAsync(userId, "001", "chave-da-conta", default))
            .ReturnsAsync(account);

        var result = await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx"), default);

        result.AccountId.Should().Be(account.Id);
        result.AccountName.Should().Be("Nubank");
        result.AccountResolution.Should().Be(AccountResolutions.Recognized);
        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b => b.AccountId == account.Id), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveUsarContaPrincipal_QuandoNadaIdentificaAConta()
    {
        var userId = Guid.NewGuid();
        SetupOfx();

        var result = await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx"), default);

        result.AccountResolution.Should().Be(AccountResolutions.Defaulted);
        result.AccountName.Should().Be(Account.DefaultName);
    }

    [Fact]
    public async Task Handle_DeveAprenderChaveDaConta_QuandoUsuarioEscolheContaSemChave()
    {
        var userId = Guid.NewGuid();
        var chosen = NewAccount(userId);
        SetupOfx();
        _accountRepository.Setup(r => r.GetByIdAsync(chosen.Id, userId, default)).ReturnsAsync(chosen);

        var result = await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx", chosen.Id), default);

        result.AccountResolution.Should().Be(AccountResolutions.Learned);
        chosen.AccountKeyHash.Should().Be("chave-da-conta");
        chosen.BankId.Should().Be("001");
        chosen.AccountLast4.Should().Be("1234");
        _accountRepository.Verify(r => r.UpdateAsync(chosen, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoArquivoPertenceAOutraContaDoUsuario()
    {
        var userId = Guid.NewGuid();
        var chosen = NewAccount(userId, "Itaú");
        var owner = NewAccount(userId, "Nubank", key: "chave-da-conta");
        SetupOfx();
        _accountRepository.Setup(r => r.GetByIdAsync(chosen.Id, userId, default)).ReturnsAsync(chosen);
        _accountRepository.Setup(r => r.GetByKeyAsync(userId, "001", "chave-da-conta", default)).ReturnsAsync(owner);

        var act = async () => await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx", chosen.Id), default);

        (await act.Should().ThrowAsync<FinanceFlow.Application.Common.Exceptions.ValidationException>())
            .WithMessage("*Nubank*");
        _bankImportRepository.Verify(r => r.AddAsync(It.IsAny<BankImport>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoContaEscolhidaEstaArquivada()
    {
        var userId = Guid.NewGuid();
        var chosen = NewAccount(userId, archived: true);
        SetupOfx();
        _accountRepository.Setup(r => r.GetByIdAsync(chosen.Id, userId, default)).ReturnsAsync(chosen);

        var act = async () => await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx", chosen.Id), default);

        await act.Should().ThrowAsync<FinanceFlow.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_DeveLancarNotFound_QuandoContaEscolhidaNaoExiste()
    {
        var userId = Guid.NewGuid();
        SetupOfx();

        var act = async () => await CreateHandler().Handle(
            new UploadOFXCommand(userId, new MemoryStream(), "extrato.ofx", Guid.NewGuid()), default);

        await act.Should().ThrowAsync<FinanceFlow.Application.Common.Exceptions.NotFoundException>();
    }

    // Linhas idênticas no mesmo arquivo recebem hashes distintos (1ª, 2ª ocorrência)
    [Fact]
    public async Task Handle_DeveGerarHashesDistintos_ParaLinhasIdenticasNoMesmoArquivo()
    {
        var date = new DateTime(2026, 11, 10);
        var transactions = new List<OFXTransaction>
        {
            new("", date, -9.50m, "CAFE", "DEBIT"),
            new("", date, -9.50m, "CAFE", "DEBIT"),
            new("", date, -9.50m, "CAFE", "DEBIT"),
        };

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", date, date, transactions));
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        var command = new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx");

        await CreateHandler().Handle(command, default);

        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.Transactions.Select(t => t.Hash).Distinct().Count() == 3),
            default), Times.Once);
    }

    // A 1ª ocorrência mantém o hash antigo, compatível com lançamentos já importados
    [Fact]
    public async Task Handle_DeveManterHashAntigo_NaPrimeiraOcorrencia()
    {
        var date = new DateTime(2026, 11, 10);
        var expected = FinanceFlow.Application.Services.ImportHashing
            .ComputeBaseHash(date, -9.50m, "CAFE", "DEBIT");

        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("123", "001", date, date,
                [new("", date, -9.50m, "CAFE", "DEBIT"), new("", date, -9.50m, "CAFE", "DEBIT")]));
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        await CreateHandler().Handle(
            new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx"), default);

        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b => b.Transactions.First().Hash == expected), default), Times.Once);
    }

    // A identificação do arquivo (em hash) é guardada para validar/aprender a conta ao trocá-la no preview
    [Fact]
    public async Task Handle_DeveGuardarIdentificacaoDoArquivoNaImportacao()
    {
        var date = new DateTime(2026, 11, 10);
        _ofxParserService
            .Setup(s => s.Parse(It.IsAny<Stream>()))
            .Returns(new OFXParseResult("12345-6", "001", date, date,
                [new("FIT1", date, -10m, "CAFE", "DEBIT")]));
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(default)).ReturnsAsync([]);

        await CreateHandler().Handle(
            new UploadOFXCommand(Guid.NewGuid(), new MemoryStream(), "extrato.ofx"), default);

        _bankImportRepository.Verify(r =>
            r.AddAsync(It.Is<BankImport>(b =>
                b.BankId == "001" &&
                b.AccountKeyHash == "chave-da-conta" &&
                b.AccountLast4 == "1234"),
            default), Times.Once);
    }
}
