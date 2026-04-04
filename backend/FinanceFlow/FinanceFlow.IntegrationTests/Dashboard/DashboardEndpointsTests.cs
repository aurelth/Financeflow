using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Dashboard;

public class DashboardEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Dashboard",
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

    // GET /api/dashboard/summary

    [Fact]
    public async Task GetSummary_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/dashboard/summary?month=3&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "summary.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/summary?month=3&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        result.Should().NotBeNull();
        result!.Month.Should().Be(3);
        result.Year.Should().Be(2026);
    }

    [Fact]
    public async Task GetSummary_DeveRetornarZerosQuandoNaoHaTransacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "summary.zero.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/summary?month=3&year=2026");
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        result!.TotalIncome.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.Balance.Should().Be(0);
        result.ProjectedBalance.Should().Be(0);
    }

    // GET /api/dashboard/balance-evolution

    [Fact]
    public async Task GetBalanceEvolution_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/dashboard/balance-evolution?month=3&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalanceEvolution_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "evolution.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/balance-evolution?month=1&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BalanceEvolutionDto>>();
        result.Should().NotBeNull();
    }

    // GET /api/dashboard/expenses-by-category

    [Fact]
    public async Task GetExpensesByCategory_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/dashboard/expenses-by-category?month=3&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExpensesByCategory_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "expenses.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/expenses-by-category?month=3&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<ExpensesByCategoryDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExpensesByCategory_DeveRetornarListaVaziaQuandoNaoHaDespesas()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "expenses.zero.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/expenses-by-category?month=3&year=2026");
        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<ExpensesByCategoryDto>>();

        result.Should().BeEmpty();
    }

    // GET /api/dashboard/weekly-comparison

    [Fact]
    public async Task GetWeeklyComparison_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/dashboard/weekly-comparison?month=3&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeeklyComparison_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "weekly.dashboard@teste.com");

        var response = await client.GetAsync("/api/dashboard/weekly-comparison?month=3&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<WeeklyComparisonDto>>();

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
    }
}
