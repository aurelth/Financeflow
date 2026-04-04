using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Transactions;

public class TransactionDueEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Due",
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
        await (await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequestDto(
            Name: "TesteDue_Unico",
            Icon: "💰",
            Color: "#f59e0b",
            Type: TransactionType.Expense)))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private static async Task CreateScheduledTransactionAsync(
        HttpClient client,
        Guid categoryId,
        DateTime date)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("500.00"),                          "amount"         },
            { new StringContent("2"),                               "type"           },
            { new StringContent(date.ToString("o")),                "date"           },
            { new StringContent("Conta agendada"),                  "description"    },
            { new StringContent("3"),                               "status"         },
            { new StringContent("false"),                           "isRecurring"    },
            { new StringContent("0"),                               "recurrenceType" },
            { new StringContent(categoryId.ToString()),             "categoryId"     },
        };

        await client.PostAsync("/api/transactions", form);
    }

    // GET /api/transactions/internal/due

    [Fact]
    public async Task GetDueInternal_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/transactions/internal/due?targetDate={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDueInternal_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "due.getall@teste.com");

        // Act
        var response = await client.GetAsync(
            $"/api/transactions/internal/due?targetDate={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<DueTransactionDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDueInternal_DeveRetornarTransacoes_QuandoExistemVencimentos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "due.exist@teste.com");

        var category = await CreateCategoryAsync(client);
        var targetDate = DateTime.UtcNow.AddDays(1).Date;

        await CreateScheduledTransactionAsync(client, category!.Id, targetDate);

        // Act
        var response = await client.GetAsync(
            $"/api/transactions/internal/due?targetDate={targetDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<DueTransactionDto>>();
        result.Should().HaveCount(c => c >= 1);
    }

    [Fact]
    public async Task GetDueInternal_DeveRetornarListaVazia_QuandoNaoExistemVencimentos()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await AuthenticateAsync(client, "due.empty@teste.com");

        // Act — data no futuro distante sem transações
        var response = await client.GetAsync(
            "/api/transactions/internal/due?targetDate=2099-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<DueTransactionDto>>();
        result.Should().BeEmpty();
    }
}
