using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Budgets;

public class BudgetEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private static readonly CreateCategoryRequestDto ValidCategoryRequest = new(
        Name: "TesteBudget_Unico",
        Icon: "💰",
        Color: "#f59e0b",
        Type: TransactionType.Expense);

    private HttpClient CreateAuthenticatedClient() => factory.CreateClient();

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

    private static async Task<CategoryDto?> CreateCategoryAsync(HttpClient client) =>
        await (await client.PostAsJsonAsync("/api/categories", ValidCategoryRequest))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private static async Task<BudgetDto?> CreateBudgetAsync(
        HttpClient client,
        Guid categoryId,
        int month = 3,
        int year = 2026) =>
        await (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequestDto(
            CategoryId: categoryId,
            Month: month,
            Year: year,
            LimitAmount: 500.00m)))
            .Content.ReadFromJsonAsync<BudgetDto>();

    // GET /api/budgets

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/budgets?month=3&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "getall.budget@teste.com");

        // Act
        var response = await client.GetAsync("/api/budgets?month=3&year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BudgetDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_DeveRetornarOrcamentos_QuandoExistemParaOPeriodo()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "getall.period.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        await CreateBudgetAsync(client, category!.Id, month: 3, year: 2026);

        // Act
        var response = await client.GetAsync("/api/budgets?month=3&year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BudgetDto>>();
        result.Should().HaveCount(1);
        result!.First().LimitAmount.Should().Be(500.00m);
    }

    // GET /api/budgets/summary

    [Fact]
    public async Task GetSummary_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "summary.budget@teste.com");

        // Act
        var response = await client.GetAsync("/api/budgets/summary?month=3&year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BudgetSummaryDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummary_DeveCalcularPercentagem_QuandoExistemTransacoes()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "summary.calc.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        await CreateBudgetAsync(client, category!.Id, month: 3, year: 2026);

        var form = new MultipartFormDataContent
        {
            { new StringContent("250.00"), "amount" },
            { new StringContent("2"), "type" },
            { new StringContent(new DateTime(2026, 3, 15).ToString("o")), "date" },
            { new StringContent("Supermercado"), "description" },
            { new StringContent("1"), "status" },
            { new StringContent("false"), "isRecurring" },
            { new StringContent("0"), "recurrenceType" },
            { new StringContent(category.Id.ToString()), "categoryId" }
        };

        await client.PostAsync("/api/transactions", form);

        // Act
        var response = await client.GetAsync("/api/budgets/summary?month=3&year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BudgetSummaryDto>>();

        var summary = result!.First();
        summary.SpentAmount.Should().Be(250.00m);
        summary.LimitAmount.Should().Be(500.00m);
        summary.Percentage.Should().Be(50.00m);
    }

    // POST /api/budgets

    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "create.budget@teste.com");

        var category = await CreateCategoryAsync(client);

        var request = new CreateBudgetRequestDto(
            CategoryId: category!.Id,
            Month: 3,
            Year: 2026,
            LimitAmount: 1000.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<BudgetDto>();
        result.Should().NotBeNull();
        result!.LimitAmount.Should().Be(1000.00m);
        result.CategoryId.Should().Be(category.Id);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoOrcamentoDuplicado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "create.dup.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        await CreateBudgetAsync(client, category!.Id, month: 3, year: 2026);

        var request = new CreateBudgetRequestDto(
            CategoryId: category.Id,
            Month: 3,
            Year: 2026,
            LimitAmount: 800.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoDadosInvalidos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "create.invalid.budget@teste.com");

        var request = new CreateBudgetRequestDto(
            CategoryId: Guid.Empty,
            Month: 13,
            Year: 2026,
            LimitAmount: -100.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT /api/budgets/{id}

    [Fact]
    public async Task Update_DeveRetornar200_QuandoDadosSaoValidos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "update.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        var budget = await CreateBudgetAsync(client, category!.Id);

        var request = new UpdateBudgetRequestDto(LimitAmount: 1500.00m);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/budgets/{budget!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BudgetDto>();
        result.Should().NotBeNull();
        result!.LimitAmount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task Update_DeveRetornar404_QuandoOrcamentoNaoExiste()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "update404.budget@teste.com");

        var request = new UpdateBudgetRequestDto(LimitAmount: 1500.00m);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DELETE /api/budgets/{id}

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoOrcamentoExiste()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "delete.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        var budget = await CreateBudgetAsync(client, category!.Id);

        // Act
        var response = await client.DeleteAsync($"/api/budgets/{budget!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoOrcamentoNaoExiste()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "delete404.budget@teste.com");

        // Act
        var response = await client.DeleteAsync($"/api/budgets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveRemoverOrcamento_QuandoExiste()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "delete.confirm.budget@teste.com");

        var category = await CreateCategoryAsync(client);
        var budget = await CreateBudgetAsync(client, category!.Id);

        // Act
        await client.DeleteAsync($"/api/budgets/{budget!.Id}");

        // Assert
        var getResponse = await client.GetAsync("/api/budgets?month=3&year=2026");
        var result = await getResponse.Content
            .ReadFromJsonAsync<IEnumerable<BudgetDto>>();
        result.Should().NotContain(b => b.Id == budget.Id);
    }
}
