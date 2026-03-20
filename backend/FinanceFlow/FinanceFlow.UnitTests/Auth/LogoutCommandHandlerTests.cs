using FinanceFlow.Application.UseCases.Auth.Commands.Logout;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();

    private LogoutCommandHandler CreateHandler() =>
        new(_refreshTokenService.Object);

    [Fact]
    public async Task Handle_DeveRevogarRefreshToken_QuandoLogout()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new LogoutCommand(userId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _refreshTokenService.Verify(r =>
            r.RevokeAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveLancarExcecao_QuandoTokenNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new LogoutCommand(userId);

        _refreshTokenService
            .Setup(r => r.RevokeAsync(userId, default))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
