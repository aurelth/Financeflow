using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Enums;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Reports;

public class ReportEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Reports",
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
    }

    // GET /api/reports

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/reports");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "getall.reports@teste.com");

        var response = await client.GetAsync("/api/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<ReportDto>>();
        result.Should().NotBeNull();
    }

    // POST /api/reports/request

    [Fact]
    public async Task Request_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 3, Year: 2026));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_DeveRetornar201_QuandoDadosSaoValidos()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "request.reports@teste.com");

        var response = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 3, Year: 2026));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ReportDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ReportStatus.Pending);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Request_DeveRetornar422_QuandoMesInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "request.invalid.reports@teste.com");

        var response = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 13, Year: 2026));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // GET /api/reports/{id}/download

    [Fact]
    public async Task Download_DeveRetornar404_QuandoReportNaoExiste()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "download.reports@teste.com");

        var response = await client.GetAsync($"/api/reports/{Guid.NewGuid()}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_DeveRetornar422_QuandoReportNaoEstaCompleto()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "download.pending.reports@teste.com");

        // Cria relatório com status Pending
        var requestResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 3, Year: 2026));

        var report = await requestResponse.Content.ReadFromJsonAsync<ReportDto>();

        // Tenta baixar — deve retornar 422 pois ainda está Pending
        var response = await client.GetAsync($"/api/reports/{report!.Id}/download");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
