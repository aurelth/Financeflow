using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Transactions;

public class RecurringTransactionEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private static async Task<HttpClient> CreateAuthenticatedClientAsync(
        FinanceFlowWebApplicationFactory factory,
        string email)
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Aurel Recorrente",
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

        return client;
    }

    private static async Task<CategoryDto?> CreateCategoryAsync(HttpClient client) =>
        await (await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequestDto(
            Name: "TesteRecorrente_Unico",
            Icon: "🔁",
            Color: "#6366f1",
            Type: TransactionType.Expense)))
            .Content.ReadFromJsonAsync<CategoryDto>();

    // Cria segunda categoria para testar propagação de categoria
    private static async Task<CategoryDto?> CreateSecondCategoryAsync(HttpClient client) =>
        await (await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequestDto(
            Name: "TesteRecorrente_Unico2",
            Icon: "💡",
            Color: "#f59e0b",
            Type: TransactionType.Expense)))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private static MultipartFormDataContent BuildRecurringTransactionForm(
        Guid categoryId,
        DateTime date,
        bool isRecurring = true)
    {
        return new MultipartFormDataContent
        {
            { new StringContent("100.00"),                              "amount"         },
            { new StringContent("2"),                                   "type"           },
            { new StringContent(date.ToString("o")),                    "date"           },
            { new StringContent("Assinatura mensal"),                   "description"    },
            { new StringContent("1"),                                   "status"         },
            { new StringContent(isRecurring.ToString().ToLower()),      "isRecurring"    },
            { new StringContent("3"),                                   "recurrenceType" },
            { new StringContent(categoryId.ToString()),                 "categoryId"     },
        };
    }

    // POST /api/transactions — criação recorrente

    [Fact]
    public async Task Create_DeveGerarCopiasRecorrentes_QuandoIsRecurringTrue()
    {
        // Arrange — cria em Outubro, espera cópias para Nov e Dez
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.create@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var form = BuildRecurringTransactionForm(category!.Id, date);

        // Act
        var response = await client.PostAsync("/api/transactions", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Should().HaveCount(3);
        list.Items.Should().ContainSingle(t => t.Status == TransactionStatus.Paid);
        list.Items.Where(t => t.Status == TransactionStatus.Scheduled).Should().HaveCount(2);
        list.Items.Should().OnlyContain(t => t.RecurrenceGroupId != null);
        list.Items.Select(t => t.RecurrenceGroupId).Distinct().Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_NaoDeveGerarCopias_QuandoIsRecurringFalse()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.false@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var form = BuildRecurringTransactionForm(category!.Id, date, isRecurring: false);

        // Act
        var response = await client.PostAsync("/api/transactions", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Should().HaveCount(1);
        list.Items.First().RecurrenceGroupId.Should().BeNull();
    }

    // PUT /api/transactions/{id} — propagação de edição

    [Fact]
    public async Task Update_DevePropagar_QuandoPropagateToFutureTrue()
    {
        // Arrange — cria recorrente em Outubro, edita com propagação
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.update@teste.com");
        var category = await CreateCategoryAsync(client);
        var category2 = await CreateSecondCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var createResponse = await client.PostAsync("/api/transactions",
            BuildRecurringTransactionForm(category!.Id, date));

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var updateRequest = new UpdateTransactionRequestDto(
            Amount: 999.00m,
            Type: TransactionType.Expense,
            Date: date,
            Description: "Assinatura atualizada",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: category2!.Id,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: true);

        // Act
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/transactions/{created!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        // Valida Amount, Description e CategoryId na propagação
        list!.Items.Should().OnlyContain(t =>
            t.Amount == 999.00m &&
            t.Description == "Assinatura atualizada" &&
            t.CategoryId == category2.Id);
    }

    [Fact]
    public async Task Update_NaoDevePropagar_QuandoPropagateToFutureFalse()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.update.false@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var createResponse = await client.PostAsync("/api/transactions",
            BuildRecurringTransactionForm(category!.Id, date));

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var updateRequest = new UpdateTransactionRequestDto(
            Amount: 777.00m,
            Type: TransactionType.Expense,
            Date: date,
            Description: "Apenas esta",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: category.Id,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: false);

        // Act
        await client.PutAsJsonAsync($"/api/transactions/{created!.Id}", updateRequest);

        // Assert — apenas a transação editada deve ter o novo valor
        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Count(t => t.Amount == 777.00m).Should().Be(1);
        list.Items.Count(t => t.Amount == 100.00m).Should().Be(2);
    }

    [Fact]
    public async Task Update_DeveAlterarStatusApenasNaAtual_QuandoPropagateToFutureTrue()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.status@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var createResponse = await client.PostAsync("/api/transactions",
            BuildRecurringTransactionForm(category!.Id, date));

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var updateRequest = new UpdateTransactionRequestDto(
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: date,
            Description: "Assinatura mensal",
            Status: TransactionStatus.Pending,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: category.Id,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: true);

        // Act
        await client.PutAsJsonAsync($"/api/transactions/{created!.Id}", updateRequest);

        // Assert — apenas a atual deve ter status Pending, futuras continuam Scheduled
        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Count(t => t.Status == TransactionStatus.Pending).Should().Be(1);
        list.Items.Count(t => t.Status == TransactionStatus.Scheduled).Should().Be(2);
    }

    // DELETE /api/transactions/{id} — remoção em cascata

    [Fact]
    public async Task Delete_DeveRemoverApenasAtual_QuandoDeleteFutureFalse()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.delete.false@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var createResponse = await client.PostAsync("/api/transactions",
            BuildRecurringTransactionForm(category!.Id, date));

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        // Act
        var deleteResponse = await client.DeleteAsync(
            $"/api/transactions/{created!.Id}?deleteFuture=false");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_DeveRemoverAtualEFuturas_QuandoDeleteFutureTrue()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(factory, "recorrente.delete.true@teste.com");
        var category = await CreateCategoryAsync(client);

        var date = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var createResponse = await client.PostAsync("/api/transactions",
            BuildRecurringTransactionForm(category!.Id, date));

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        // Act
        var deleteResponse = await client.DeleteAsync(
            $"/api/transactions/{created!.Id}?deleteFuture=true");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/transactions?PageSize=50");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<TransactionDto>>();

        list!.Items.Should().BeEmpty();
    }
}
