using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Settings;

public class SettingsEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Settings",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    // POST /api/settings/logout-all

    [Fact]
    public async Task LogoutAll_DeveRetornar204_QuandoAutenticado()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.logoutall@teste.com");

        // Act
        var response = await client.PostAsync("/api/settings/logout-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogoutAll_DeveRetornar401_QuandoNaoAutenticado()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsync("/api/settings/logout-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // DELETE /api/settings/account

    [Fact]
    public async Task DeleteAccount_DeveRetornar204_QuandoSenhaCorreta()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.delete@teste.com");

        var body = new { CurrentPassword = "Teste@123" };

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/settings/account")
        {
            Content = JsonContent.Create(body)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAccount_DeveRetornar422_QuandoSenhaIncorreta()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.delete.wrongpwd@teste.com");

        var body = new { CurrentPassword = "SenhaErrada" };

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/settings/account")
        {
            Content = JsonContent.Create(body)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteAccount_DeveRetornar401_QuandoNaoAutenticado()
    {
        // Arrange
        var client = CreateClient();

        var body = new { CurrentPassword = "Teste@123" };

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/settings/account")
        {
            Content = JsonContent.Create(body)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
