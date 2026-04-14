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

        var auth = await loginResponse.Content.ReadAsJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    // GET /api/settings/notifications

    [Fact]
    public async Task GetNotifications_DeveRetornar200_ComPreferenciasPadrao()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.getnotif@teste.com");

        // Act
        var response = await client.GetAsync("/api/settings/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<NotificationPreferencesDto>();
        result.Should().NotBeNull();
        result!.BudgetWarningEnabled.Should().BeTrue();
        result.BudgetCriticalEnabled.Should().BeTrue();
        result.TransactionDueTomorrowEnabled.Should().BeTrue();
        result.TransactionDueIn3DaysEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotifications_DeveRetornar401_QuandoNaoAutenticado()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/settings/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // PUT /api/settings/notifications

    [Fact]
    public async Task UpdateNotifications_DeveRetornar204_QuandoDadosValidos()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.updatenotif@teste.com");

        var body = new UpdateNotificationPreferencesRequestDto(
            BudgetWarningEnabled: false,
            BudgetCriticalEnabled: true,
            TransactionDueTomorrowEnabled: false,
            TransactionDueIn3DaysEnabled: true
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings/notifications", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateNotifications_DevePersistirAlteracoes()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsync(client, "settings.updatenotif.persist@teste.com");

        var body = new UpdateNotificationPreferencesRequestDto(
            BudgetWarningEnabled: false,
            BudgetCriticalEnabled: false,
            TransactionDueTomorrowEnabled: false,
            TransactionDueIn3DaysEnabled: false
        );

        // Act
        await client.PutAsJsonAsync("/api/settings/notifications", body);
        var response = await client.GetAsync("/api/settings/notifications");

        // Assert
        var result = await response.Content.ReadAsJsonAsync<NotificationPreferencesDto>();
        result!.BudgetWarningEnabled.Should().BeFalse();
        result.BudgetCriticalEnabled.Should().BeFalse();
        result.TransactionDueTomorrowEnabled.Should().BeFalse();
        result.TransactionDueIn3DaysEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotifications_DeveRetornar401_QuandoNaoAutenticado()
    {
        // Arrange
        var client = CreateClient();

        var body = new UpdateNotificationPreferencesRequestDto(
            BudgetWarningEnabled: false,
            BudgetCriticalEnabled: false,
            TransactionDueTomorrowEnabled: false,
            TransactionDueIn3DaysEnabled: false
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings/notifications", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    [Fact]
    public async Task DeleteAccount_DevePermitirNovaConta_ComEmailDeContaExcluida()
    {
        // Arrange
        var client = CreateClient();
        var email = "settings.reuse.email@teste.com";
        await AuthenticateAsync(client, email);

        // Exclui a conta
        await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/settings/account")
        {
            Content = JsonContent.Create(new { CurrentPassword = "Teste@123" })
        });

        // Act — tenta criar nova conta com o mesmo email
        var newClient = CreateClient();
        var response = await newClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequestDto(
                Name: "Aurel Reuse",
                Email: email,
                Password: "Teste@123",
                Cpf: TestCpfGenerator.Next(),
                Gender: "Male",
                Currency: "BRL",
                Timezone: "America/Sao_Paulo"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_DeveRetornar401_QuandoContaExcluida()
    {
        // Arrange
        var client = CreateClient();
        var email = "settings.login.deleted@teste.com";
        await AuthenticateAsync(client, email);

        // Exclui a conta
        await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/settings/account")
        {
            Content = JsonContent.Create(new { CurrentPassword = "Teste@123" })
        });

        // Act — tenta fazer login com conta excluída
        var newClient = CreateClient();
        var response = await newClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
