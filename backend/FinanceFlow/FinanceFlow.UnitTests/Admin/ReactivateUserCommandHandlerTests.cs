using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Admin.Commands.ReactivateUser;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Admin;

public class ReactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ReactivateUserCommandHandler _handler;

    public ReactivateUserCommandHandlerTests()
    {
        _handler = new ReactivateUserCommandHandler(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_DeveReativarUsuario_QuandoDesativado()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User, DeletedAt = DateTime.UtcNow };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ReactivateUserCommand(user.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepository.Verify(r => r.ReactivateAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoUsuarioJaAtivo()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.User };

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ReactivateUserCommand(user.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userRepository.Verify(r => r.ReactivateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoUsuarioNaoEncontrado()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ReactivateUserCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
