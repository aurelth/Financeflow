using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly IMapper _mapper;

    private static readonly string ValidCpf = "529.982.247-25"; // CPF válido para testes

    public RegisterUserCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<UserMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private RegisterUserCommandHandler CreateHandler() =>
        new(_userRepository.Object, _passwordService.Object, _mapper);

    private RegisterUserCommand BuildCommand(
        string email = "aurel@teste.com",
        string? currency = "BRL",
        string? timezone = "America/Sao_Paulo") =>
        new("Aurel Teste", email, "Teste@123", ValidCpf, Gender.Male, currency, timezone);

    [Fact]
    public async Task Handle_DeveRegistarUtilizador_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = BuildCommand();

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(command.Email, default))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.ExistsByCpfAsync(command.Cpf, default))
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
        var command = BuildCommand();

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
    public async Task Handle_DeveLancarValidationException_QuandoCpfJaExiste()
    {
        // Arrange
        var command = BuildCommand();

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(command.Email, default))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.ExistsByCpfAsync(command.Cpf, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*CPF*");
    }

    [Fact]
    public async Task Handle_DeveNormalizarEmail_QuandoEmailTemMaiusculas()
    {
        // Arrange
        var command = BuildCommand(email: "AUREL@TESTE.COM");

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), default))
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
        var command = BuildCommand(currency: null, timezone: null);

        _userRepository
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), default))
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
