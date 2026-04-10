using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Admin.Commands.DemoteUser;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Admin;

public class DemoteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly DemoteUserCommandHandler _handler;

    public DemoteUserCommandHandlerTests()
    {
        _handler = new DemoteUserCommandHandler(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_DeveRebaixarAdmin_QuandoValido()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = new DemoteUserCommand(user.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserRole.User, user.Role);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
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

        var command = new DemoteUserCommand(user.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUsuarioNaoEAdmin()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new DemoteUserCommand(user.Id);

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

        var command = new DemoteUserCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
