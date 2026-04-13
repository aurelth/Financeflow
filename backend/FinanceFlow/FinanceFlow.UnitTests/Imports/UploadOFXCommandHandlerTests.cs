using System.Security.Cryptography;
using System.Text;
using FinanceFlow.Application.Common.Interfaces;
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
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly IConfiguration _configuration;

    public UploadOFXCommandHandlerTests()
    {
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
}
