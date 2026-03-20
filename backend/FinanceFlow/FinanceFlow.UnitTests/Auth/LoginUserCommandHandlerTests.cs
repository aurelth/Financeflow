using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Auth.Commands.LoginUser;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly IMapper _mapper;

    public LoginUserCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<UserMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private LoginUserCommandHandler CreateHandler() =>
        new(_userRepository.Object,
            _passwordService.Object,
            _tokenService.Object,
            _refreshTokenService.Object,
            _mapper);

    [Fact]
    public async Task Handle_DeveRetornarTokens_QuandoCredenciaisSaoValidas()
    {
        // Arrange
        var user = UserBuilder.Build();
        var command = new LoginUserCommand(user.Email, "Teste@123");

        _userRepository
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        _tokenService
            .Setup(t => t.GenerateAccessToken(user))
            .Returns("access_token");

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(900);
        result.User.Email.Should().Be(user.Email);

        _refreshTokenService.Verify(r =>
            r.SaveAsync(user.Id, "refresh_token", default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarUnauthorizedException_QuandoEmailNaoExiste()
    {
        // Arrange
        var command = new LoginUserCommand("inexistente@teste.com", "Teste@123");

        _userRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Domain.Entities.User?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Email ou senha incorreto*");
    }

    [Fact]
    public async Task Handle_DeveLancarUnauthorizedException_QuandoPasswordErrada()
    {
        // Arrange
        var user = UserBuilder.Build();
        var command = new LoginUserCommand(user.Email, "senha_errada");

        _userRepository
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Email ou senha incorreto*");
    }
}
