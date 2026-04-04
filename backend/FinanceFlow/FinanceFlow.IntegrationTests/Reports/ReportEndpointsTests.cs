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
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Reports",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        // Debug temporário
        if (!registerResponse.IsSuccessStatusCode)
        {
            var body = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Register falhou: {registerResponse.StatusCode} - {body}");
        }

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

    // DELETE /api/reports/{id}

    [Fact]
    public async Task Delete_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"/api/reports/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoRelatorioNaoExiste()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "delete.notfound.reports@teste.com");

        var response = await client.DeleteAsync($"/api/reports/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoRelatorioExiste()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "delete.exists.reports@teste.com");

        // Cria relatório
        var requestResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 3, Year: 2026));
        var report = await requestResponse.Content.ReadFromJsonAsync<ReportDto>();

        // Deleta
        var response = await client.DeleteAsync($"/api/reports/{report!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // Validação de mudanças nas transações

    [Fact]
    public async Task Request_DeveRetornar422_QuandoJaExisteRelatorioSemMudancas()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "request.nomudanca.reports@teste.com");

        // Solicita primeiro relatório — vai ficar Pending (sem Worker)
        // Para simular Completed, usamos status update interno
        var requestResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 1, Year: 2020));

        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var report = await requestResponse.Content.ReadFromJsonAsync<ReportDto>();

        // Atualiza para Completed via endpoint interno
        await client.PutAsJsonAsync($"/api/reports/{report!.Id}/status",
            new UpdateReportStatusDto("Completed", "storage/test.csv", "test.csv"));

        // Tenta gerar novo relatório para o mesmo período sem mudanças
        var secondResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 1, Year: 2020));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Request_DevePermitir_QuandoRelatorioAnteriorFoiDeletado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "request.deleted.reports@teste.com");

        // Solicita primeiro relatório
        var requestResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 1, Year: 2021));

        var report = await requestResponse.Content.ReadFromJsonAsync<ReportDto>();

        // Atualiza para Completed
        await client.PutAsJsonAsync($"/api/reports/{report!.Id}/status",
            new UpdateReportStatusDto("Completed", "storage/test.csv", "test.csv"));

        // Deleta o relatório
        await client.DeleteAsync($"/api/reports/{report.Id}");

        // Tenta gerar novo relatório — deve ser permitido pois o anterior foi deletado
        var secondResponse = await client.PostAsJsonAsync("/api/reports/request",
            new CreateReportRequestDto(Month: 1, Year: 2021));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
