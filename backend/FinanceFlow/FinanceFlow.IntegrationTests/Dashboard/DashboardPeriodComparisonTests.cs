using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Dashboard;

public class DashboardPeriodComparisonTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Comparison",
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

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=2026-01&periods=2026-02");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.auth@teste.com");

        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=2026-01&periods=2026-02");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PeriodComparisonDto>();
        result.Should().NotBeNull();
        result!.Periods.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar400_QuandoSemPeriodos()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.noperiod@teste.com");

        var response = await client.GetAsync("/api/dashboard/period-comparison");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar400_QuandoMaisDe3Periodos()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.max@teste.com");

        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=2026-01&periods=2026-02&periods=2026-03&periods=2026-04");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar400_QuandoPeriodoInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.invalid@teste.com");

        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=invalido");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornar200_Com3Periodos()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.three@teste.com");

        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=2026-01&periods=2026-02&periods=2026-03");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PeriodComparisonDto>();
        result!.Periods.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPeriodComparison_DeveRetornarCategoriasvazias_QuandoNaoHaTransacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "comparison.empty@teste.com");

        var response = await client.GetAsync(
            "/api/dashboard/period-comparison?periods=2020-01&periods=2020-02");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PeriodComparisonDto>();
        result!.CategoryComparisons.Should().BeEmpty();
    }
}
