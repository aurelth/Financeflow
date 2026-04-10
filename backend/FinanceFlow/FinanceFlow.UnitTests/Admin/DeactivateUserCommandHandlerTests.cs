using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Admin.Commands.DeactivateUser;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Admin;

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests()
    {
        _handler = new DeactivateUserCommandHandler(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_DeveDesativarUsuario_QuandoValido()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new DeactivateUserCommand(user.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepository.Verify(r => r.DeleteAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUsuarioJaDesativado()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User, DeletedAt = DateTime.UtcNow };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new DeactivateUserCommand(user.Id, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUnicoAdmin()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeactivateUserCommand(user.Id, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoUsuarioNaoEncontrado()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new DeactivateUserCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
