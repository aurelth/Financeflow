using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Auth.Commands.ForgotPassword;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IConfiguration> _configuration = new();

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(_userRepository.Object,
            _passwordResetTokenRepository.Object,
            _emailService.Object,
            _configuration.Object);

    public ForgotPasswordCommandHandlerTests()
    {
        _configuration
            .Setup(c => c["Frontend:BaseUrl"])
            .Returns("http://localhost:3000");
    }

    [Fact]
    public async Task Handle_DeveEnviarEmail_QuandoUsuarioExiste()
    {
        // Arrange
        var user = UserBuilder.Build();
        var command = new ForgotPasswordCommand(user.Email);

        _userRepository
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _passwordResetTokenRepository
            .Setup(r => r.InvalidateUserTokensAsync(user.Id, default))
            .Returns(Task.CompletedTask);

        _passwordResetTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default))
            .Returns(Task.CompletedTask);

        _emailService
            .Setup(e => e.SendPasswordResetEmailAsync(
                user.Email, user.Name, It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _passwordResetTokenRepository.Verify(r =>
            r.AddAsync(It.IsAny<PasswordResetToken>(), default), Times.Once);

        _emailService.Verify(e =>
            e.SendPasswordResetEmailAsync(
                user.Email, user.Name, It.IsAny<string>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveEnviarEmail_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var command = new ForgotPasswordCommand("inexistente@teste.com");

        _userRepository
            .Setup(r => r.GetByEmailAsync(command.Email, default))
            .ReturnsAsync((Domain.Entities.User?)null);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — não deve lançar exceção nem enviar email
        _emailService.Verify(e =>
            e.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);

        _passwordResetTokenRepository.Verify(r =>
            r.AddAsync(It.IsAny<PasswordResetToken>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveInvalidarTokensAnteriores_QuandoUsuarioExiste()
    {
        // Arrange
        var user = UserBuilder.Build();
        var command = new ForgotPasswordCommand(user.Email);

        _userRepository
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _passwordResetTokenRepository
            .Setup(r => r.InvalidateUserTokensAsync(user.Id, default))
            .Returns(Task.CompletedTask);

        _passwordResetTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default))
            .Returns(Task.CompletedTask);

        _emailService
            .Setup(e => e.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _passwordResetTokenRepository.Verify(r =>
            r.InvalidateUserTokensAsync(user.Id, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveGerarTokenComExpiracao1Hora()
    {
        // Arrange
        var user = UserBuilder.Build();
        var command = new ForgotPasswordCommand(user.Email);

        PasswordResetToken? captured = null;

        _userRepository
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _passwordResetTokenRepository
            .Setup(r => r.InvalidateUserTokensAsync(user.Id, default))
            .Returns(Task.CompletedTask);

        _passwordResetTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default))
            .Callback<PasswordResetToken, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        _emailService
            .Setup(e => e.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.Id);
        captured.IsUsed.Should().BeFalse();
        captured.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
    }
}
