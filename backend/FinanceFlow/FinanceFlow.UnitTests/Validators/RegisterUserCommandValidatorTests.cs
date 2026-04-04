using FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static readonly string ValidCpf = "529.982.247-25";
    private static readonly Gender ValidGender = Gender.Male;

    private RegisterUserCommand BuildCommand(
        string name = "Aurel Teste",
        string email = "aurel@teste.com",
        string password = "Teste@123",
        string? cpf = null,
        Gender? gender = null) =>
        new(name, email, password, cpf ?? ValidCpf, gender ?? ValidGender, "BRL", "America/Sao_Paulo");

    [Fact]
    public void Validate_DeveSerValido_QuandoDadosCorretos()
    {
        var result = _validator.Validate(BuildCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "aurel@teste.com", "Teste@123")]
    [InlineData("A", "aurel@teste.com", "Teste@123")]
    public void Validate_DeveSerInvalido_QuandoNomeInvalido(
        string name, string email, string password)
    {
        var result = _validator.Validate(BuildCommand(name: name, email: email, password: password));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("Aurel Teste", "", "Teste@123")]
    [InlineData("Aurel Teste", "email-invalido", "Teste@123")]
    public void Validate_DeveSerInvalido_QuandoEmailInvalido(
        string name, string email, string password)
    {
        var result = _validator.Validate(BuildCommand(name: name, email: email, password: password));
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
        var result = _validator.Validate(BuildCommand(name: name, email: email, password: password));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123.456.789-00")]   // CPF inválido
    [InlineData("111.111.111-11")]   // CPF com todos dígitos iguais
    [InlineData("12345678900")]      // CPF sem formatação
    public void Validate_DeveSerInvalido_QuandoCpfInvalido(string cpf)
    {
        var result = _validator.Validate(BuildCommand(cpf: cpf));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void Validate_DeveSerValido_QuandoCpfValido()
    {
        var result = _validator.Validate(BuildCommand(cpf: "529.982.247-25"));
        result.Errors.Should().NotContain(e => e.PropertyName == "Cpf");
    }
}
