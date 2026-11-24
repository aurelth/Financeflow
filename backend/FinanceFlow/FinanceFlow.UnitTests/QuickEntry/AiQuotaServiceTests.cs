using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services.Ai;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace FinanceFlow.UnitTests.QuickEntry;

public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public class AiQuotaServiceTests
{
    private readonly Mock<IAiUsageRepository> _usage = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Guid _userId = Guid.NewGuid();

    private AiQuotaService CreateService(DateTimeOffset nowUtc, int limit = 30) =>
        new(_usage.Object, _users.Object,
            Options.Create(new AiOptions { DailyLimit = limit }),
            new FixedTimeProvider(nowUtc));

    private void UserWithTimezone(string? timezone) =>
        _users.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, Timezone = timezone! });

    [Fact]
    public async Task Acquire_DeveUsarODiaDoUsuario_NaoODiaEmUtc()
    {
        // 01:30 UTC de 24/11 ainda é 22:30 de 23/11 em São Paulo
        UserWithTimezone("America/Sao_Paulo");
        _usage.Setup(r => r.TryConsumeAsync(_userId, new DateOnly(2026, 11, 23), 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lease = await CreateService(new DateTimeOffset(2026, 11, 24, 1, 30, 0, TimeSpan.Zero))
            .AcquireAsync(_userId);

        lease.Day.Should().Be(new DateOnly(2026, 11, 23));
        lease.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Acquire_DeveLancarQuotaExceeded_ComALimpezaNaMeiaNoiteDoUsuario()
    {
        UserWithTimezone("America/Sao_Paulo");
        _usage.Setup(r => r.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => CreateService(new DateTimeOffset(2026, 11, 24, 1, 30, 0, TimeSpan.Zero), limit: 5)
            .AcquireAsync(_userId);

        var ex = (await act.Should().ThrowAsync<QuotaExceededException>()).Which;
        ex.Limit.Should().Be(5);
        ex.Message.Should().Contain("5");
        // Meia-noite de 24/11 em São Paulo (UTC-3) = 03:00 UTC
        ex.ResetsAtUtc.Should().Be(new DateTime(2026, 11, 24, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Acquire_DeveBloquearSemTocarNoBanco_QuandoOLimiteEZero()
    {
        UserWithTimezone("America/Sao_Paulo");

        var act = () => CreateService(DateTimeOffset.UtcNow, limit: 0).AcquireAsync(_userId);

        var ex = (await act.Should().ThrowAsync<QuotaExceededException>()).Which;
        ex.Message.Should().Contain("não está disponível");
        _usage.Verify(r => r.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Fuso/Que/Nao/Existe")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Acquire_NaoDeveFalhar_ComFusoInvalido(string? timezone)
    {
        UserWithTimezone(timezone);
        _usage.Setup(r => r.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Cai no padrão do produto (São Paulo): 01:30 UTC ainda é 23/11
        var lease = await CreateService(new DateTimeOffset(2026, 11, 24, 1, 30, 0, TimeSpan.Zero)).AcquireAsync(_userId);

        lease.Day.Should().Be(new DateOnly(2026, 11, 23));
    }

    [Fact]
    public async Task Acquire_DeveFuncionar_QuandoOUsuarioNaoExiste()
    {
        _users.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _usage.Setup(r => r.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lease = await CreateService(DateTimeOffset.UtcNow).AcquireAsync(_userId);

        lease.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetStatus_DeveCalcularORestante()
    {
        UserWithTimezone("America/Sao_Paulo");
        _usage.Setup(r => r.GetCountAsync(_userId, new DateOnly(2026, 11, 24), It.IsAny<CancellationToken>())).ReturnsAsync(12);

        var status = await CreateService(new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero), limit: 30)
            .GetStatusAsync(_userId);

        status.Limit.Should().Be(30);
        status.Used.Should().Be(12);
        status.Remaining.Should().Be(18);
        status.ResetsAtUtc.Should().Be(new DateTime(2026, 11, 25, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetStatus_NuncaDeveRetornarRestanteNegativo_QuandoOLimiteBaixou()
    {
        UserWithTimezone("America/Sao_Paulo");
        _usage.Setup(r => r.GetCountAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(40);

        var status = await CreateService(DateTimeOffset.UtcNow, limit: 30).GetStatusAsync(_userId);

        status.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task Release_DeveDevolverAChamadaNoDiaDaReserva()
    {
        var lease = new AiQuotaLease(_userId, new DateOnly(2026, 11, 23));

        await CreateService(DateTimeOffset.UtcNow).ReleaseAsync(lease);

        _usage.Verify(r => r.ReleaseAsync(_userId, new DateOnly(2026, 11, 23), It.IsAny<CancellationToken>()), Times.Once);
    }
}
