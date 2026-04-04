using System.Net;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Auth;

public class PasswordResetEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Reset",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",                 
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));
    }

    [Fact]
    public async Task ForgotPassword_DeveRetornar204_QuandoEmailExiste()
    {
        var client = CreateClient();
        await RegisterAsync(client, "forgot.exist@teste.com");

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("forgot.exist@teste.com"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_DeveRetornar204_QuandoEmailNaoExiste()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("inexistente@teste.com"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_DeveRetornar422_QuandoEmailInvalido()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("email-invalido"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoTokenInvalido()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "token-invalido",
                NewPassword: "NovaSenha@123",
                ConfirmPassword: "NovaSenha@123"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoSenhasFraca()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "qualquer-token",
                NewPassword: "fraca",
                ConfirmPassword: "fraca"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoSenhasNaoCoincidem()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "qualquer-token",
                NewPassword: "NovaSenha@123",
                ConfirmPassword: "Diferente@123"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
