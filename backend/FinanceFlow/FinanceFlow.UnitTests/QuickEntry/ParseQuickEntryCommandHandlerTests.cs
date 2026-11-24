using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Services.Ai;
using FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SkiaSharp;

namespace FinanceFlow.UnitTests.QuickEntry;

public class ParseQuickEntryCommandHandlerTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);

    private readonly Mock<IAiQuotaService> _quota = new();
    private readonly Mock<IAnthropicService> _ai = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<ICategoryRuleRepository> _rules = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly AiQuotaLease _lease;
    private readonly Category _food = new() { Id = Guid.NewGuid(), Name = "Alimentação", Type = TransactionType.Expense };
    private readonly Category _market = new() { Id = Guid.NewGuid(), Name = "Mercado", Type = TransactionType.Expense };

    public ParseQuickEntryCommandHandlerTests()
    {
        _lease = new AiQuotaLease(_userId, Today);

        _quota.Setup(q => q.AcquireAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(_lease);
        _quota.Setup(q => q.GetStatusAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiQuotaStatus(30, 1, new DateTime(2026, 11, 25, 3, 0, 0, DateTimeKind.Utc)));

        _categories.Setup(c => c.GetAllByUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_food, _market]);
        _rules.Setup(r => r.GetByKeysAsync(_userId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private ParseQuickEntryCommandHandler CreateHandler() =>
        new(_quota.Object, _ai.Object, _categories.Object, _rules.Object,
            Options.Create(new AiOptions()), NullLogger<ParseQuickEntryCommandHandler>.Instance);

    private void AiReplies(string reply) =>
        _ai.Setup(a => a.SendMessageWithImagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reply);

    private string OneExpense(Guid? categoryId = null, string description = "Almoço iFood") =>
        $$"""{"entries":[{"type":"Expense","amount":45.9,"date":"2026-11-23","description":"{{description}}","categoryId":{{(categoryId is null ? "null" : $"\"{categoryId}\"")}}}]}""";

    private static byte[] Png()
    {
        using var bitmap = new SKBitmap(64, 64);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Fact]
    public async Task Handle_DeveDevolverRascunhosECotaAtualizada()
    {
        AiReplies(OneExpense(_food.Id));

        var result = await CreateHandler().Handle(
            new ParseQuickEntryCommand(_userId, "almoço 45,90 no ifood ontem", null), default);

        var draft = result.Drafts.Should().ContainSingle().Subject;
        draft.Amount.Should().Be(45.9m);
        draft.Date.Should().Be(new DateTime(2026, 11, 23));
        draft.CategoryId.Should().Be(_food.Id);
        draft.CategoryName.Should().Be("Alimentação");
        result.Quota.Should().Be(new FinanceFlow.Application.DTOs.QuickEntry.AiQuotaDto(
            30, 1, 29, new DateTime(2026, 11, 25, 3, 0, 0, DateTimeKind.Utc)));

        _quota.Verify(q => q.ReleaseAsync(It.IsAny<AiQuotaLease>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveEnviarCategoriasEDataNoPromptESemImagem_QuandoSoTexto()
    {
        string? system = null;
        string? user = null;
        IReadOnlyList<AiImage>? images = null;
        _ai.Setup(a => a.SendMessageWithImagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<AiImage>, CancellationToken>((s, u, i, _) => (system, user, images) = (s, u, i))
            .ReturnsAsync("{\"entries\":[]}");

        await CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "uber 20", null), default);

        system.Should().Contain("2026-11-24").And.Contain($"{_food.Id} | Expense | Alimentação");
        user.Should().Contain("uber 20");
        images.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeveEnviarAImagemNormalizadaEmJpeg()
    {
        IReadOnlyList<AiImage>? images = null;
        _ai.Setup(a => a.SendMessageWithImagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<AiImage>, CancellationToken>((_, _, i, _) => images = i)
            .ReturnsAsync("{\"entries\":[]}");

        await CreateHandler().Handle(new ParseQuickEntryCommand(_userId, null, Png()), default);

        var sent = images.Should().ContainSingle().Subject;
        sent.MediaType.Should().Be("image/jpeg");
        sent.Data.Take(2).Should().Equal(0xFF, 0xD8); // assinatura JPEG: foi regravada, não repassada
    }

    [Fact]
    public async Task Handle_NaoDeveCobrarCota_QuandoNaoHaLancamentoNoTexto()
    {
        AiReplies("{\"entries\":[]}");

        var result = await CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "bom dia", null), default);

        // A IA foi chamada (custo real): a chamada vale mesmo sem rascunhos
        result.Drafts.Should().BeEmpty();
        _quota.Verify(q => q.ReleaseAsync(It.IsAny<AiQuotaLease>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePreferirARegraAprendida_AoPalpiteDaIA()
    {
        AiReplies(OneExpense(_food.Id, "Mercado Extra"));
        _rules.Setup(r => r.GetByKeysAsync(_userId, It.Is<IReadOnlyCollection<string>>(k => k.Contains("mercado extra")), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CategoryRule.Create(_userId, TransactionType.Expense, "mercado extra", _market.Id)]);

        var result = await CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "mercado extra 80", null), default);

        result.Drafts.Single().CategoryId.Should().Be(_market.Id);
        result.Drafts.Single().CategoryName.Should().Be("Mercado");
    }

    [Fact]
    public async Task Handle_DeveIgnorarRegraDeCategoriaQueNaoEstaMaisDisponivel()
    {
        AiReplies(OneExpense(_food.Id, "Mercado Extra"));
        _rules.Setup(r => r.GetByKeysAsync(_userId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CategoryRule.Create(_userId, TransactionType.Expense, "mercado extra", Guid.NewGuid())]);

        var result = await CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "mercado extra 80", null), default);

        result.Drafts.Single().CategoryId.Should().Be(_food.Id);
    }

    [Fact]
    public async Task Handle_DeveDevolverACota_QuandoAIAFalha()
    {
        _ai.Setup(a => a.SendMessageWithImagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("anthropic fora do ar"));

        var act = () => CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "café 5", null), default);

        await act.Should().ThrowAsync<HttpRequestException>();
        _quota.Verify(q => q.ReleaseAsync(_lease, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDevolverACota_QuandoARespostaNaoEEntendivel()
    {
        AiReplies("desculpe, não entendi");

        var act = () => CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "café 5", null), default);

        await act.Should().ThrowAsync<ValidationException>();
        _quota.Verify(q => q.ReleaseAsync(_lease, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDevolverACotaMesmoCancelado_ENaoUsarOTokenCancelado()
    {
        using var cts = new CancellationTokenSource();
        _ai.Setup(a => a.SendMessageWithImagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()))
            .Returns(() => { cts.Cancel(); throw new OperationCanceledException(cts.Token); });

        var act = () => CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "café 5", null), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _quota.Verify(q => q.ReleaseAsync(_lease, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveChamarAIANemConsultarCategorias_QuandoACotaAcabou()
    {
        _quota.Setup(q => q.AcquireAsync(_userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new QuotaExceededException("limite", 30, DateTime.UtcNow.AddHours(3)));

        var act = () => CreateHandler().Handle(new ParseQuickEntryCommand(_userId, "café 5", null), default);

        await act.Should().ThrowAsync<QuotaExceededException>();
        _ai.Verify(a => a.SendMessageWithImagesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<AiImage>>(), It.IsAny<CancellationToken>()), Times.Never);
        _categories.Verify(c => c.GetAllByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        // Nada foi reservado, nada a devolver
        _quota.Verify(q => q.ReleaseAsync(It.IsAny<AiQuotaLease>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NaoDeveGastarCota_QuandoAImagemEInvalida()
    {
        var act = () => CreateHandler().Handle(
            new ParseQuickEntryCommand(_userId, null, "não sou uma foto"u8.ToArray()), default);

        await act.Should().ThrowAsync<ValidationException>();
        _quota.Verify(q => q.AcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
