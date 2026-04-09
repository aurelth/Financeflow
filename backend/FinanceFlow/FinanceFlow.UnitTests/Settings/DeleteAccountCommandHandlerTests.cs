using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Settings.Commands.DeleteAccount;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Settings;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _handler = new DeleteAccountCommandHandler(
            _userRepository.Object,
            _passwordService.Object,
            _refreshTokenService.Object);
    }

    [Fact]
    public async Task Handle_DeveExcluirContaQuandoSenhaCorreta()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Aurel Teste",
            Email = "aurel@teste.com",
            PasswordHash = "hash",
        };

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService.Setup(p => p.Verify("Senha@123", "hash"))
            .Returns(true);

        var command = new DeleteAccountCommand(userId, "Senha@123");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenService.Verify(
            s => s.RevokeAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepository.Verify(
            r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationExceptionQuandoSenhaIncorreta()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Aurel Teste",
            Email = "aurel@teste.com",
            PasswordHash = "hash",
        };

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService.Setup(p => p.Verify("SenhaErrada", "hash"))
            .Returns(false);

        var command = new DeleteAccountCommand(userId, "SenhaErrada");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _refreshTokenService.Verify(
            s => s.RevokeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundExceptionQuandoUtilizadorNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new DeleteAccountCommand(userId, "Senha@123");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
