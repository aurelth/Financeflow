using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly IMapper _mapper;

    public RegisterUserCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<UserMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private RegisterUserCommandHandler CreateHandler() =>
        new(_userRepository.Object, _passwordService.Object, _mapper);

    [Fact]
    public async Task Handle_DeveRegistarUtilizador_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel Teste", "aurel@teste.com", "Teste@123", "BRL", "America/Sao_Paulo");

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(command.Email, default))
            .ReturnsAsync(false);

        _passwordService
            .Setup(p => p.Hash(command.Password))
            .Returns("hashed_password");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(command.Email);
        result.Name.Should().Be(command.Name);
        result.Currency.Should().Be("BRL");

        _userRepository.Verify(r =>
            r.AddAsync(It.IsAny<Domain.Entities.User>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoEmailJaExiste()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel Teste", "aurel@teste.com", "Teste@123", null, null);

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(command.Email, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*email*");
    }

    [Fact]
    public async Task Handle_DeveNormalizarEmail_QuandoEmailTemMaiusculas()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel Teste", "AUREL@TESTE.COM", "Teste@123", null, null);

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _passwordService
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Email.Should().Be("aurel@teste.com");
    }

    [Fact]
    public async Task Handle_DeveUsarValoresPadrao_QuandoCurrencyETimezoneNaoInformados()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel Teste", "aurel@teste.com", "Teste@123", null, null);

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _passwordService
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Currency.Should().Be("BRL");
        result.Timezone.Should().Be("America/Sao_Paulo");
    }
}
