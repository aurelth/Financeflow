using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Admin.Commands.PromoteUser;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Admin;

public class PromoteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly PromoteUserCommandHandler _handler;

    public PromoteUserCommandHandlerTests()
    {
        _handler = new PromoteUserCommandHandler(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_DevePromoverUsuario_QuandoValido()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new PromoteUserCommand(user.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUsuarioJaEAdmin()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new PromoteUserCommand(user.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUsuarioDesativado()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User, DeletedAt = DateTime.UtcNow };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new PromoteUserCommand(user.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoUsuarioNaoEncontrado()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new PromoteUserCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
