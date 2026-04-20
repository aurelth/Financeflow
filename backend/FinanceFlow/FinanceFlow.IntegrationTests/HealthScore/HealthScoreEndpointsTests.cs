using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.HealthScore;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanceFlow.IntegrationTests.HealthScore;

public class HealthScoreEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
        Name: "Aurel HealthScore",
        Email: "healthscore@teste.com",
        Password: "Teste@123",
        Cpf: TestCpfGenerator.Next(),
        Gender: "Male",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo");

    private async Task AuthenticateAsync(string email = "healthscore@teste.com")
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            ValidRegisterRequest with
            {
                Email = email,
                Cpf = TestCpfGenerator.Next()
            });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadAsJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    // GET /api/healthscore

    [Fact]
    public async Task Get_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.GetAsync("/api/healthscore");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("healthscore.get@teste.com");

        // Act
        var response = await _client.GetAsync("/api/healthscore");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<HealthScoreResult>();
        result.Should().NotBeNull();
        result!.Score.Should().BeInRange(0, 100);
        result.Classification.Should().NotBeNullOrEmpty();
        result.Details.Should().HaveCount(5);
    }

    [Fact]
    public async Task Get_DeveRetornar200_QuandoMesEAnoInformados()
    {
        // Arrange
        await AuthenticateAsync("healthscore.periodo@teste.com");

        // Act
        var response = await _client.GetAsync("/api/healthscore?month=1&year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<HealthScoreResult>();
        result.Should().NotBeNull();
        result!.Score.Should().BeInRange(0, 100);
    }

    // GET /api/healthscore/history

    [Fact]
    public async Task GetHistory_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.GetAsync("/api/healthscore/history");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("healthscore.history@teste.com");

        // Act
        var response = await _client.GetAsync("/api/healthscore/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadAsJsonAsync<IEnumerable<HealthScoreHistoryItem>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetHistory_DeveRetornarScoresComClassificacao_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("healthscore.history2@teste.com");

        // Act
        var response = await _client.GetAsync("/api/healthscore/history");
        var result = await response.Content
            .ReadAsJsonAsync<IEnumerable<HealthScoreHistoryItem>>();

        // Assert
        result.Should().OnlyContain(h =>
            h.Score >= 0 &&
            h.Score <= 100 &&
            !string.IsNullOrEmpty(h.Classification) &&
            !string.IsNullOrEmpty(h.MonthLabel));
    }
}
