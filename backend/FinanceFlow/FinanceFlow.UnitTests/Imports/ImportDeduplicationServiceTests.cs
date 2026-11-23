using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class ImportDeduplicationServiceTests
{
    private readonly Mock<IBankImportRepository> _repository = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    private ImportDeduplicationService CreateService() => new(_repository.Object);

    private BankImport NewImport(params BankImportTransaction[] rows) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        AccountId = _accountId,
        Transactions = rows,
    };

    private static BankImportTransaction Row(
        string hash,
        string fitId = "",
        decimal amount = 100m,
        TransactionType type = TransactionType.Expense,
        DateTime? date = null) => new()
    {
        Id = Guid.NewGuid(),
        Hash = hash,
        ExternalId = fitId,
        Amount = amount,
        Type = type,
        Date = date ?? new DateTime(2026, 11, 10),
    };

    private void SetupKeys(params ImportedTransactionKey[] keys) =>
        _repository
            .Setup(r => r.GetImportedKeysAsync(
                _userId, _accountId,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(keys);

    private void SetupManual(params Transaction[] manual) =>
        _repository
            .Setup(r => r.GetManualCandidatesAsync(
                _userId, _accountId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(manual);

    [Fact]
    public async Task DetectAsync_DeveMarcarPorFitId_QuandoMesmoFitIdEValor()
    {
        var existingId = Guid.NewGuid();
        var row = Row("h1", fitId: "FIT-1", amount: 100m);
        SetupKeys(new ImportedTransactionKey(existingId, "outro-hash", "FIT-1", 100m));
        SetupManual();

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(1);
        row.IsDuplicate.Should().BeTrue();
        row.IsSelected.Should().BeFalse();
        row.DuplicateReason.Should().Be(DuplicateReason.SameFitId);
        row.MatchedTransactionId.Should().Be(existingId);
    }

    // Alguns bancos reaproveitam o FITID: com valor diferente não é duplicata
    [Fact]
    public async Task DetectAsync_NaoDeveMarcar_QuandoMesmoFitIdMasValorDiferente()
    {
        var row = Row("h1", fitId: "FIT-1", amount: 100m);
        SetupKeys(new ImportedTransactionKey(Guid.NewGuid(), "outro-hash", "FIT-1", 55m));
        SetupManual();

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(0);
        row.IsDuplicate.Should().BeFalse();
        row.IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task DetectAsync_DeveMarcarPorConteudo_QuandoHashJaExisteNaConta()
    {
        var existingId = Guid.NewGuid();
        var row = Row("h1");
        SetupKeys(new ImportedTransactionKey(existingId, "h1", null, 100m));
        SetupManual();

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(1);
        row.DuplicateReason.Should().Be(DuplicateReason.SameContent);
        row.MatchedTransactionId.Should().Be(existingId);
    }

    // Duas linhas idênticas no arquivo têm hashes distintos (ocorrência 1 e 2):
    // se só a primeira já foi importada, a segunda continua sendo nova
    [Fact]
    public async Task DetectAsync_DeveMarcarSoAPrimeiraOcorrencia_QuandoSoUmaJaFoiImportada()
    {
        var primeira = Row(ImportHashing.WithOccurrence("base", 1));
        var segunda = Row(ImportHashing.WithOccurrence("base", 2));
        SetupKeys(new ImportedTransactionKey(Guid.NewGuid(), primeira.Hash, null, 100m));
        SetupManual();

        var count = await CreateService().DetectAsync(NewImport(primeira, segunda));

        count.Should().Be(1);
        primeira.IsDuplicate.Should().BeTrue();
        segunda.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task DetectAsync_DeveMarcarProvavelLancamentoManual_QuandoMesmoValorTipoEDataProxima()
    {
        var manual = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 11, 9),
            Description = "Mercado",
        };
        var row = Row("h1", date: new DateTime(2026, 11, 10));
        SetupKeys();
        SetupManual(manual);

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(1);
        row.IsDuplicate.Should().BeTrue();
        row.IsSelected.Should().BeFalse();
        row.DuplicateReason.Should().Be(DuplicateReason.PossibleManual);
        row.MatchedTransactionId.Should().Be(manual.Id);
    }

    [Theory]
    [InlineData(150, "Expense", 10)] // valor diferente
    [InlineData(100, "Income", 10)]  // tipo diferente
    [InlineData(100, "Expense", 20)] // data fora da tolerância
    public async Task DetectAsync_NaoDeveMarcarComoManual_QuandoCriterioNaoBate(
        decimal manualAmount, string manualType, int manualDay)
    {
        var manual = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = manualAmount,
            Type = Enum.Parse<TransactionType>(manualType),
            Date = new DateTime(2026, 11, manualDay),
        };
        var row = Row("h1", date: new DateTime(2026, 11, 10));
        SetupKeys();
        SetupManual(manual);

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(0);
        row.IsDuplicate.Should().BeFalse();
    }

    // Um lançamento manual só explica uma linha do extrato
    [Fact]
    public async Task DetectAsync_DeveUsarCadaLancamentoManualUmaUnicaVez()
    {
        var manual = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 11, 10),
        };
        var a = Row("a", date: new DateTime(2026, 11, 10));
        var b = Row("b", date: new DateTime(2026, 11, 10));
        SetupKeys();
        SetupManual(manual);

        var count = await CreateService().DetectAsync(NewImport(a, b));

        count.Should().Be(1);
        new[] { a, b }.Count(r => r.IsDuplicate).Should().Be(1);
    }

    // Reprocessar (ex.: troca de conta) zera as marcações anteriores
    [Fact]
    public async Task DetectAsync_DeveLimparMarcacoesAnteriores_AoReprocessar()
    {
        var row = Row("h1");
        row.IsDuplicate = true;
        row.IsSelected = false;
        row.DuplicateReason = DuplicateReason.SameContent;
        row.MatchedTransactionId = Guid.NewGuid();
        SetupKeys();
        SetupManual();

        var count = await CreateService().DetectAsync(NewImport(row));

        count.Should().Be(0);
        row.IsDuplicate.Should().BeFalse();
        row.IsSelected.Should().BeTrue();
        row.DuplicateReason.Should().BeNull();
        row.MatchedTransactionId.Should().BeNull();
    }

    [Fact]
    public async Task DetectAsync_DeveConsultarSomenteContaDaImportacao()
    {
        var row = Row("h1");
        SetupKeys();
        SetupManual();

        await CreateService().DetectAsync(NewImport(row));

        _repository.Verify(r => r.GetImportedKeysAsync(
            _userId, _accountId,
            It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
