using FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_DeveSerValido_QuandoDadosCorretos()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel Teste", "aurel@teste.com", "Teste@123", "BRL", "America/Sao_Paulo");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "aurel@teste.com", "Teste@123")]
    [InlineData("A", "aurel@teste.com", "Teste@123")]
    public void Validate_DeveSerInvalido_QuandoNomeInvalido(
        string name, string email, string password)
    {
        // Arrange
        var command = new RegisterUserCommand(name, email, password, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("Aurel Teste", "", "Teste@123")]
    [InlineData("Aurel Teste", "email-invalido", "Teste@123")]
    public void Validate_DeveSerInvalido_QuandoEmailInvalido(
        string name, string email, string password)
    {
        // Arrange
        var command = new RegisterUserCommand(name, email, password, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("Aurel Teste", "aurel@teste.com", "fraca")]
    [InlineData("Aurel Teste", "aurel@teste.com", "semmaiuscula1@")]
    [InlineData("Aurel Teste", "aurel@teste.com", "SemNumero@")]
    [InlineData("Aurel Teste", "aurel@teste.com", "SemSimbolo1")]
    public void Validate_DeveSerInvalido_QuandoPasswordFraca(
        string name, string email, string password)
    {
        // Arrange
        var command = new RegisterUserCommand(name, email, password, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
