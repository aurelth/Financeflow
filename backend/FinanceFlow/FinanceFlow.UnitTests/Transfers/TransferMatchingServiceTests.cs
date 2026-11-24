using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transfers;

public class TransferMatchingServiceTests
{
    private readonly Mock<ITransferRepository> _repository = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountA = Guid.NewGuid();
    private readonly Guid _accountB = Guid.NewGuid();

    private static readonly DateTime Day = new(2026, 2, 12);

    private TransferMatchingService CreateService() => new(_repository.Object);

    private BankImport NewImport(params BankImportTransaction[] rows) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        AccountId = _accountA,
        Transactions = rows,
    };

    private static BankImportTransaction Row(
        string description,
        decimal amount,
        TransactionType type,
        DateTime? date = null,
        bool duplicate = false) => new()
    {
        Id = Guid.NewGuid(),
        Description = description,
        Amount = amount,
        Type = type,
        Date = date ?? Day,
        IsDuplicate = duplicate,
        Hash = Guid.NewGuid().ToString("N"),
    };

    private Transaction Candidate(
        string description,
        decimal amount,
        TransactionType type,
        DateTime? date = null,
        TransferDirection? direction = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        AccountId = _accountB,
        Description = description,
        Amount = amount,
        Type = type,
        TransferDirection = direction,
        Date = date ?? Day,
    };

    private void SetupCandidates(params Transaction[] candidates) =>
        _repository
            .Setup(r => r.GetCandidatesAsync(
                _userId, _accountA,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

    // Pontuação

    [Fact]
    public void Score_DeveSerZero_QuandoValoresDiferem()
    {
        var score = TransferMatchingService.Score(100m, Day, "PIX", 99.99m, Day, "PIX", false);

        score.Should().Be(0);
    }

    [Fact]
    public void Score_DeveSerZero_QuandoPassaDeTresDias()
    {
        var score = TransferMatchingService.Score(100m, Day, "PIX", 100m, Day.AddDays(4), "PIX", true);

        score.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 45)]
    [InlineData(1, 35)]
    [InlineData(2, 25)]
    [InlineData(3, 15)]
    public void Score_DeveDarMenosPontos_ConformeAsDatasSeAfastam(int daysApart, int expected)
    {
        var score = TransferMatchingService.Score(
            100m, Day, "Compra", 100m, Day.AddDays(daysApart), "Outra compra", false);

        score.Should().Be(expected);
    }

    [Fact]
    public void Score_DeveSomarPalavrasChave_ENaturezaDeTransferencia()
    {
        // mesma data (45) + as duas descrições (35) + já é transferência (20)
        var score = TransferMatchingService.Score(
            100m, Day, "PIX ENVIADO", 100m, Day, "PIX RECEBIDO", true);

        score.Should().Be(100);
    }

    [Fact]
    public void Score_DeveDarMenosPontos_QuandoSoUmaDescricaoSugereTransferencia()
    {
        var score = TransferMatchingService.Score(
            100m, Day, "PIX ENVIADO", 100m, Day, "Compra", false);

        score.Should().Be(45 + 15);
    }

    [Theory]
    [InlineData("PIX ENVIADO JOAO", true)]
    [InlineData("Transferência recebida", true)]
    [InlineData("TED-TRANSF 123", true)]
    [InlineData("DOC 99", true)]
    [InlineData("SUPERMERCADO TEDESCO", false)]
    [InlineData("PRESENTED BY", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void LooksLikeTransfer_DeveReconhecerPalavrasInteiras(string? description, bool expected)
    {
        TransferMatchingService.LooksLikeTransfer(description).Should().Be(expected);
    }

    [Theory]
    [InlineData(TransactionType.Income, null, TransferDirection.In)]
    [InlineData(TransactionType.Expense, null, TransferDirection.Out)]
    [InlineData(TransactionType.Transfer, TransferDirection.Out, TransferDirection.Out)]
    [InlineData(TransactionType.Transfer, TransferDirection.In, TransferDirection.In)]
    public void DirectionOf_DeveSeguirOTipoOuADirecaoGravada(
        TransactionType type, TransferDirection? stored, TransferDirection expected)
    {
        TransferMatchingService.DirectionOf(type, stored).Should().Be(expected);
    }

    [Fact]
    public void DirectionOf_DeveSerNulo_ParaTransferenciaLegadaSemDirecao()
    {
        TransferMatchingService.DirectionOf(TransactionType.Transfer, null).Should().BeNull();
    }

    // Pareamento

    [Fact]
    public async Task MatchAsync_DeveParearAutomaticamente_QuandoPixEntreContasNoMesmoDia()
    {
        // O Pix enviado da conta A e a receita "PIX RECEBIDO" já lançada na conta B
        var row = Row("PIX ENVIADO CONTA B", 1000m, TransactionType.Expense);
        var income = Candidate("PIX RECEBIDO CONTA A", 1000m, TransactionType.Income);
        SetupCandidates(income);

        var matched = await CreateService().MatchAsync(NewImport(row));

        matched.Should().Be(1);
        row.TransferMatchTransactionId.Should().Be(income.Id);
        row.TransferMatchStatus.Should().Be(TransferMatchStatus.Auto);
        row.TransferMatchScore.Should().Be(80);
    }

    [Fact]
    public async Task MatchAsync_DeveParearAutomaticamente_QuandoOutraPontaJaEhTransferenciaPendente()
    {
        // Ordem inversa: a conta A já tem a saída como transferência sem par e agora chega a entrada na B
        var row = Row("PIX RECEBIDO CONTA A", 1000m, TransactionType.Income);
        var pendingOut = Candidate("PIX ENVIADO CONTA B", 1000m, TransactionType.Transfer,
            direction: TransferDirection.Out);
        SetupCandidates(pendingOut);

        await CreateService().MatchAsync(NewImport(row));

        row.TransferMatchTransactionId.Should().Be(pendingOut.Id);
        row.TransferMatchStatus.Should().Be(TransferMatchStatus.Auto);
        row.TransferMatchScore.Should().Be(100);
    }

    [Fact]
    public async Task MatchAsync_DeveSugerirApenas_QuandoNaoHaPalavraChaveDeTransferencia()
    {
        // mesma data (45) + só uma descrição com palavra-chave (15) = 60
        var row = Row("PIX RECEBIDO JOAO", 200m, TransactionType.Income);
        var expense = Candidate("Compra qualquer", 200m, TransactionType.Expense);
        SetupCandidates(expense);

        await CreateService().MatchAsync(NewImport(row));

        row.TransferMatchStatus.Should().Be(TransferMatchStatus.Suggested);
        row.TransferMatchScore.Should().Be(60);
    }

    [Fact]
    public async Task MatchAsync_NaoDeveParear_QuandoApenasValorEDataCoincidem()
    {
        // O mercado de R$ 50 e uma receita de R$ 50 no mesmo dia: 45 pontos, abaixo do mínimo
        var row = Row("MERCADO CENTRAL", 50m, TransactionType.Expense);
        SetupCandidates(Candidate("Venda de livro", 50m, TransactionType.Income));

        var matched = await CreateService().MatchAsync(NewImport(row));

        matched.Should().Be(0);
        row.TransferMatchTransactionId.Should().BeNull();
        row.TransferMatchStatus.Should().BeNull();
    }

    [Fact]
    public async Task MatchAsync_NaoDeveParear_QuandoMesmoSentido()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        SetupCandidates(Candidate("PIX ENVIADO", 100m, TransactionType.Expense));

        var matched = await CreateService().MatchAsync(NewImport(row));

        matched.Should().Be(0);
    }

    [Fact]
    public async Task MatchAsync_NaoDeveParear_QuandoCandidatoEhTransferenciaSemDirecao()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        SetupCandidates(Candidate("PIX", 100m, TransactionType.Transfer, direction: null));

        var matched = await CreateService().MatchAsync(NewImport(row));

        matched.Should().Be(0);
    }

    [Fact]
    public async Task MatchAsync_DeveRebaixarParaSugestao_QuandoHaDoisCandidatosEmpatados()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        SetupCandidates(
            Candidate("PIX RECEBIDO MARIA", 100m, TransactionType.Income),
            Candidate("PIX RECEBIDO JOSE", 100m, TransactionType.Income));

        await CreateService().MatchAsync(NewImport(row));

        row.TransferMatchTransactionId.Should().NotBeNull();
        row.TransferMatchStatus.Should().Be(TransferMatchStatus.Suggested,
            "com dois candidatos iguais o usuário precisa escolher");
    }

    [Fact]
    public async Task MatchAsync_DeveEscolherOCandidatoMaisProximoNoTempo()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        var far = Candidate("PIX RECEBIDO", 100m, TransactionType.Income, Day.AddDays(3));
        var near = Candidate("PIX RECEBIDO", 100m, TransactionType.Income, Day.AddDays(1));
        SetupCandidates(far, near);

        await CreateService().MatchAsync(NewImport(row));

        row.TransferMatchTransactionId.Should().Be(near.Id);
        row.TransferMatchStatus.Should().Be(TransferMatchStatus.Suggested, "35 + 35 = 70");
    }

    [Fact]
    public async Task MatchAsync_CadaCandidatoSoServeParaUmaLinha()
    {
        var first = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        var second = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        var only = Candidate("PIX RECEBIDO", 100m, TransactionType.Income);
        SetupCandidates(only);

        var matched = await CreateService().MatchAsync(NewImport(first, second));

        matched.Should().Be(1);
        new[] { first, second }.Count(r => r.TransferMatchTransactionId == only.Id).Should().Be(1);
        // O candidato disputado por duas linhas iguais nunca vira vínculo automático
        new[] { first, second }.Single(r => r.TransferMatchTransactionId == only.Id)
            .TransferMatchStatus.Should().Be(TransferMatchStatus.Suggested);
    }

    [Fact]
    public async Task MatchAsync_DeveIgnorarLinhasDuplicadas()
    {
        var duplicate = Row("PIX ENVIADO", 100m, TransactionType.Expense, duplicate: true);
        SetupCandidates(Candidate("PIX RECEBIDO", 100m, TransactionType.Income));

        var matched = await CreateService().MatchAsync(NewImport(duplicate));

        matched.Should().Be(0);
        _repository.Verify(r => r.GetCandidatesAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchAsync_DeveBuscarCandidatosSoEmOutrasContas_ENaJanelaDeTresDias()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense, new DateTime(2026, 2, 12));
        SetupCandidates();

        await CreateService().MatchAsync(NewImport(row));

        _repository.Verify(r => r.GetCandidatesAsync(
            _userId,
            _accountA,
            new DateTime(2026, 2, 9),
            new DateTime(2026, 2, 15, 23, 59, 59).AddTicks(9999999),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchAsync_DeveSerIdempotente_ELimparParesAnteriores()
    {
        var row = Row("PIX ENVIADO", 100m, TransactionType.Expense);
        row.TransferMatchTransactionId = Guid.NewGuid();
        row.TransferMatchScore = 99;
        row.TransferMatchStatus = TransferMatchStatus.Auto;
        SetupCandidates(); // nada na nova conta escolhida

        var matched = await CreateService().MatchAsync(NewImport(row));

        matched.Should().Be(0);
        row.TransferMatchTransactionId.Should().BeNull();
        row.TransferMatchScore.Should().BeNull();
        row.TransferMatchStatus.Should().BeNull();
    }
}
