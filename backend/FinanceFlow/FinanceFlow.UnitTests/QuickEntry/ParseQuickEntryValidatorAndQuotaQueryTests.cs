using FinanceFlow.Application.Services.Ai;
using FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;
using FinanceFlow.Application.UseCases.QuickEntry.Queries.GetAiQuota;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace FinanceFlow.UnitTests.QuickEntry;

public class ParseQuickEntryValidatorAndQuotaQueryTests
{
    private readonly ParseQuickEntryCommandValidator _validator =
        new(Options.Create(new AiOptions { MaxTextLength = 20, MaxImageBytes = 1024 }));

    [Fact]
    public void Validar_DeveAceitarSoTexto() =>
        _validator.Validate(new ParseQuickEntryCommand(Guid.NewGuid(), "café 5", null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validar_DeveAceitarSoImagem() =>
        _validator.Validate(new ParseQuickEntryCommand(Guid.NewGuid(), null, new byte[10])).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_DeveExigirTextoOuImagem(string? text)
    {
        var result = _validator.Validate(new ParseQuickEntryCommand(Guid.NewGuid(), text, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("texto ou envie a foto"));
    }

    [Fact]
    public void Validar_DeveLimitarOTexto() =>
        _validator.Validate(new ParseQuickEntryCommand(Guid.NewGuid(), new string('a', 21), null))
            .Errors.Should().Contain(e => e.ErrorMessage.Contains("20 caracteres"));

    [Fact]
    public void Validar_DeveLimitarATamanhoDaImagem() =>
        _validator.Validate(new ParseQuickEntryCommand(Guid.NewGuid(), null, new byte[1025]))
            .IsValid.Should().BeFalse();

    [Fact]
    public async Task Cota_DeveMapearOStatus()
    {
        var quota = new Mock<IAiQuotaService>();
        var userId = Guid.NewGuid();
        var resets = new DateTime(2026, 11, 25, 3, 0, 0, DateTimeKind.Utc);
        quota.Setup(q => q.GetStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiQuotaStatus(30, 12, resets));

        var dto = await new GetAiQuotaQueryHandler(quota.Object).Handle(new GetAiQuotaQuery(userId), default);

        (dto.Limit, dto.Used, dto.Remaining, dto.ResetsAtUtc).Should().Be((30, 12, 18, resets));
    }
}
