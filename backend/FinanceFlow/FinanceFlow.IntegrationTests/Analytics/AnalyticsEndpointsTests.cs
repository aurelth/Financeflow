using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Reports;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Analytics;

public class AnalyticsEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Analytics Teste",
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

    // GET /api/analytics/cash-flow

    [Fact]
    public async Task GetCashFlow_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-01-01&to=2026-03-31");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.auth@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-01-01&to=2026-03-31&groupBy=month");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<CashFlowDto>();
        result.Should().NotBeNull();
        result!.Periods.Should().HaveCount(3);
        result.GroupBy.Should().Be("month");
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornar200_ComAgrupamentoPorDia()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.day@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-01-01&to=2026-01-07&groupBy=day");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<CashFlowDto>();
        result!.Periods.Should().HaveCount(7);
        result.GroupBy.Should().Be("day");
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornar400_QuandoDataInicioMaiorQueDataFim()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.invalid@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-03-31&to=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornar400_QuandoGroupByInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.groupby@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-01-01&to=2026-03-31&groupBy=week");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornar400_QuandoDiasExcedem90PorDia()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.exceed@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2026-01-01&to=2026-06-01&groupBy=day");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCashFlow_DeveRetornarZeros_QuandoNaoHaTransacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "cashflow.empty@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/cash-flow?from=2020-01-01&to=2020-03-31&groupBy=month");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<CashFlowDto>();
        result!.TotalIncome.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.NetBalance.Should().Be(0);
    }

    // GET /api/analytics/annual-summary

    [Fact]
    public async Task GetAnnualSummary_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/analytics/annual-summary?year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnnualSummary_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "annual.auth@teste.com");

        var response = await client.GetAsync("/api/analytics/annual-summary?year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<AnnualSummaryDto>();
        result.Should().NotBeNull();
        result!.Year.Should().Be(2026);
        result.Months.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetAnnualSummary_DeveRetornar400_QuandoAnoInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "annual.invalid@teste.com");

        var response = await client.GetAsync("/api/analytics/annual-summary?year=1999");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAnnualSummary_DeveRetornarZeros_QuandoNaoHaTransacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "annual.empty@teste.com");

        var response = await client.GetAsync("/api/analytics/annual-summary?year=2020");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<AnnualSummaryDto>();
        result!.TotalIncome.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.NetBalance.Should().Be(0);
    }

    // GET /api/analytics/by-category

    [Fact]
    public async Task GetByCategory_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            "/api/analytics/by-category?from=2026-01-01&to=2026-03-31");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByCategory_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "category.auth@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-category?from=2026-01-01&to=2026-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ReportByCategoryDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCategory_DeveRetornar400_QuandoDataInicioMaiorQueDataFim()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "category.invalid@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-category?from=2026-03-31&to=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByCategory_DeveRetornarListaVazia_QuandoNaoHaTransacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "category.empty@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-category?from=2020-01-01&to=2020-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ReportByCategoryDto>();
        result!.Categories.Should().BeEmpty();
    }

    // GET /api/analytics/by-tag

    [Fact]
    public async Task GetByTag_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            "/api/analytics/by-tag?from=2026-01-01&to=2026-03-31");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByTag_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "tag.auth@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-tag?from=2026-01-01&to=2026-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ReportByTagDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTag_DeveRetornar400_QuandoDataInicioMaiorQueDataFim()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "tag.invalid@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-tag?from=2026-03-31&to=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByTag_DeveRetornarListaVazia_QuandoNaoHaTags()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "tag.empty@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/by-tag?from=2020-01-01&to=2020-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ReportByTagDto>();
        result!.Tags.Should().BeEmpty();
    }

    // GET /api/analytics/projections

    [Fact]
    public async Task GetProjections_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/analytics/projections");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjections_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "projections.auth@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/projections?monthsBack=12&monthsAhead=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ProjectionsDto>();
        result.Should().NotBeNull();
        result!.MonthsAnalysed.Should().Be(12);
        result.MonthsAhead.Should().Be(3);
        result.Historical.Should().HaveCount(12);
        result.Projected.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProjections_DeveRetornar400_QuandoMonthsBackInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "projections.invalid@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/projections?monthsBack=1&monthsAhead=3");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjections_DeveRetornar400_QuandoMonthsAheadInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "projections.ahead@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/projections?monthsBack=12&monthsAhead=10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjections_DeveRetornarProjeccoesIsprojected_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "projections.isprojected@teste.com");

        var response = await client.GetAsync(
            "/api/analytics/projections?monthsBack=6&monthsAhead=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<ProjectionsDto>();
        result!.Historical.Should().AllSatisfy(h => h.IsProjected.Should().BeFalse());
        result.Projected.Should().AllSatisfy(p => p.IsProjected.Should().BeTrue());
    }
}
