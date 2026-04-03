using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private ResetPasswordCommandHandler CreateHandler() =>
        new(_passwordResetTokenRepository.Object,
            _userRepository.Object,
            _passwordService.Object);

    private PasswordResetToken BuildValidToken(string tokenValue = "valid-token") =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

    [Fact]
    public async Task Handle_DeveRedefinirSenha_QuandoTokenValido()
    {
        // Arrange
        var token = BuildValidToken();
        var user = UserBuilder.Build(id: UserId);
        var command = new ResetPasswordCommand("valid-token", "NovaSenha@123", "NovaSenha@123");

        _passwordResetTokenRepository
            .Setup(r => r.GetValidTokenAsync("valid-token", default))
            .ReturnsAsync(token);

        _userRepository
            .Setup(r => r.GetByIdAsync(UserId, default))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.Hash("NovaSenha@123"))
            .Returns("new_hashed_password");

        _userRepository
            .Setup(r => r.UpdateAsync(user, default))
            .Returns(Task.CompletedTask);

        _passwordResetTokenRepository
            .Setup(r => r.UpdateAsync(token, default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        user.PasswordHash.Should().Be("new_hashed_password");

        _userRepository.Verify(r =>
            r.UpdateAsync(user, default), Times.Once);

        _passwordResetTokenRepository.Verify(r =>
            r.UpdateAsync(It.Is<PasswordResetToken>(t => t.IsUsed == true), default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoTokenInvalido()
    {
        // Arrange
        var command = new ResetPasswordCommand("invalid-token", "NovaSenha@123", "NovaSenha@123");

        _passwordResetTokenRepository
            .Setup(r => r.GetValidTokenAsync("invalid-token", default))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Token inválido ou expirado*");

        _userRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Domain.Entities.User>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveMarcarTokenComoUsado_AposRedefinicao()
    {
        // Arrange
        var token = BuildValidToken();
        var user = UserBuilder.Build(id: UserId);
        var command = new ResetPasswordCommand("valid-token", "NovaSenha@123", "NovaSenha@123");

        _passwordResetTokenRepository
            .Setup(r => r.GetValidTokenAsync("valid-token", default))
            .ReturnsAsync(token);

        _userRepository
            .Setup(r => r.GetByIdAsync(UserId, default))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed");

        _userRepository
            .Setup(r => r.UpdateAsync(user, default))
            .Returns(Task.CompletedTask);

        _passwordResetTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PasswordResetToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        token.IsUsed.Should().BeTrue();
    }
}
