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
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));
    }

    // POST /api/auth/forgot-password

    [Fact]
    public async Task ForgotPassword_DeveRetornar204_QuandoEmailExiste()
    {
        // Arrange
        var client = CreateClient();
        await RegisterAsync(client, "forgot.exist@teste.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("forgot.exist@teste.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_DeveRetornar204_QuandoEmailNaoExiste()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("inexistente@teste.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_DeveRetornar422_QuandoEmailInvalido()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequestDto("email-invalido"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // POST /api/auth/reset-password

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoTokenInvalido()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "token-invalido",
                NewPassword: "NovaSenha@123",
                ConfirmPassword: "NovaSenha@123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoSenhasFraca()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "qualquer-token",
                NewPassword: "fraca",
                ConfirmPassword: "fraca"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_DeveRetornar422_QuandoSenhasNaoCoincidem()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequestDto(
                Token: "qualquer-token",
                NewPassword: "NovaSenha@123",
                ConfirmPassword: "Diferente@123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
