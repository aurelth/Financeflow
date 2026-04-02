using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Notifications;

public class NotificationEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient() => factory.CreateClient();

    private static async Task<Guid> AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Notification",
            Email: email,
            Password: "Teste@123",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Extrai o UserId diretamente do JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(auth.AccessToken);
        var sub = jwt.Claims.First(c =>
            c.Type == "sub" || c.Type == JwtRegisteredClaimNames.Sub).Value;

        return Guid.Parse(sub);
    }

    private static async Task CreateNotificationAsync(
        HttpClient client,
        Guid userId,
        string type = "BudgetWarning",
        string message = "Orçamento atingiu 80%.")
    {
        await client.PostAsJsonAsync("/api/notifications", new
        {
            userId,
            type,
            message
        });
    }

    // GET /api/notifications

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "getall.notification@teste.com");

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<NotificationDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_DeveRetornarNotificacoes_QuandoExistem()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var userId = await AuthenticateAsync(client, "getall.exist.notification@teste.com");

        await CreateNotificationAsync(client, userId);
        await CreateNotificationAsync(client, userId, "BudgetCritical", "100% atingido.");

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<NotificationDto>>();
        result.Should().HaveCount(c => c >= 2);
    }

    // POST /api/notifications

    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var userId = await AuthenticateAsync(client, "create.notification@teste.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/notifications", new
        {
            userId,
            type = "BudgetWarning",
            message = "Orçamento atingiu 80%."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoDadosInvalidos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "create.invalid.notification@teste.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/notifications", new
        {
            userId = Guid.Empty,
            type = "",
            message = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PATCH /api/notifications/{id}/read

    [Fact]
    public async Task MarkAsRead_DeveRetornar404_QuandoNotificacaoNaoExiste()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "markread404.notification@teste.com");

        // Act
        var response = await client.PatchAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // PATCH /api/notifications/read-all

    [Fact]
    public async Task MarkAllAsRead_DeveRetornar204_QuandoAutenticado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "markallread.notification@teste.com");

        // Act
        var response = await client.PatchAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAllAsRead_DeveMarcarTodasComoLidas()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var userId = await AuthenticateAsync(client, "markall.confirm.notification@teste.com");

        await CreateNotificationAsync(client, userId);
        await CreateNotificationAsync(client, userId);

        // Act
        await client.PatchAsync("/api/notifications/read-all", null);

        // Assert
        var getResponse = await client.GetAsync("/api/notifications");
        var result = await getResponse.Content
            .ReadFromJsonAsync<IEnumerable<NotificationDto>>();

        result.Should().OnlyContain(n => n.IsRead == true);
    }
}
