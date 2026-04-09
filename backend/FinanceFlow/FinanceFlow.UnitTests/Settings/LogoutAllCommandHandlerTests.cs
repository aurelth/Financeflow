using FinanceFlow.Application.UseCases.Settings.Commands.LogoutAll;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Settings;

public class LogoutAllCommandHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly LogoutAllCommandHandler _handler;

    public LogoutAllCommandHandlerTests()
    {
        _handler = new LogoutAllCommandHandler(_refreshTokenService.Object);
    }

    [Fact]
    public async Task Handle_DeveRevogarTokenDoUtilizador()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new LogoutAllCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenService.Verify(
            s => s.RevokeAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
