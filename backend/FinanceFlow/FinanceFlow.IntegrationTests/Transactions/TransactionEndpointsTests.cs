using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Transactions;

public class TransactionEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
        Name: "Aurel Integration",
        Email: "transacoes@teste.com",
        Password: "Teste@123",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo"
    );

    private static readonly CreateCategoryRequestDto ValidCategoryRequest = new(
        Name: "Entretenimento",
        Icon: "🎬",
        Color: "#6366f1",
        Type: TransactionType.Expense
    );

    private async Task AuthenticateAsync(string email = "transacoes@teste.com")
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            ValidRegisterRequest with { Email = email });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    // GET /api/transactions

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.GetAsync("/api/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("getall.tx@teste.com");

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResultDto<TransactionDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_DeveRetornarTransacoesFiltradas_QuandoFiltroAplicado()
    {
        // Arrange
        await AuthenticateAsync("getall.filtro.tx@teste.com");

        var category = await CreateCategoryAsync();
        await CreateTransactionAsync(category!.Id, 50.00m);
        await CreateTransactionAsync(category.Id, 150.00m);

        // Act — filtra por valor mínimo
        var response = await _client.GetAsync("/api/transactions?AmountMin=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResultDto<TransactionDto>>();
        result!.Items.Should().OnlyContain(t => t.Amount >= 100);
    }

    // GET /api/transactions/{id}

    [Fact]
    public async Task GetById_DeveRetornar200_QuandoTransacaoExiste()
    {
        // Arrange
        await AuthenticateAsync("getbyid.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 99.90m);

        // Act
        var response = await _client.GetAsync($"/api/transactions/{transaction!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
        result.Amount.Should().Be(99.90m);
    }

    [Fact]
    public async Task GetById_DeveRetornar404_QuandoTransacaoNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("getbyid404.tx@teste.com");

        // Act
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST /api/transactions

    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("create.tx@teste.com");

        var category = await CreateCategoryAsync();

        var request = new CreateTransactionRequestDto(
            Amount: 75.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Cinema",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: category!.Id,
            SubcategoryId: null,
            Tags: ["lazer"]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        result.Should().NotBeNull();
        result!.Amount.Should().Be(75.00m);
        result.CategoryId.Should().Be(category.Id);
        result.Description.Should().Be("Cinema");
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoDadosSaoInvalidos()
    {
        // Arrange
        await AuthenticateAsync("create422.tx@teste.com");

        var request = new CreateTransactionRequestDto(
            Amount: -10.00m,        // valor inválido
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Inválido",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: Guid.Empty,     // categoria inválida
            SubcategoryId: null,
            Tags: []);

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoTipoNaoCoincideComCategoria()
    {
        // Arrange
        await AuthenticateAsync("create.tipo.tx@teste.com");

        var category = await CreateCategoryAsync(); // Type = Expense

        var request = new CreateTransactionRequestDto(
            Amount: 100.00m,
            Type: TransactionType.Income, // tipo errado
            Date: DateTime.UtcNow,
            Description: "Tipo errado",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: category!.Id,
            SubcategoryId: null,
            Tags: []);

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT /api/transactions/{id}

    [Fact]
    public async Task Update_DeveRetornar200_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("update.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 50.00m);

        var request = new UpdateTransactionRequestDto(
            Amount: 200.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Atualizado",
            Status: TransactionStatus.Pending,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: category.Id,
            SubcategoryId: null,
            Tags: ["atualizado"]);

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/transactions/{transaction!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        result.Should().NotBeNull();
        result!.Amount.Should().Be(200.00m);
        result.Description.Should().Be("Atualizado");
        result.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Update_DeveRetornar404_QuandoTransacaoNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("update404.tx@teste.com");

        var category = await CreateCategoryAsync();

        var request = new UpdateTransactionRequestDto(
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Qualquer",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: category!.Id,
            SubcategoryId: null,
            Tags: []);

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/transactions/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DELETE /api/transactions/{id}

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoTransacaoExiste()
    {
        // Arrange
        await AuthenticateAsync("delete.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 30.00m);

        // Act
        var response = await _client.DeleteAsync(
            $"/api/transactions/{transaction!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoTransacaoNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("delete404.tx@teste.com");

        // Act
        var response = await _client.DeleteAsync(
            $"/api/transactions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveAplicarSoftDelete_QuandoTransacaoExiste()
    {
        // Arrange
        await AuthenticateAsync("softdelete.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 40.00m);

        // Act
        await _client.DeleteAsync($"/api/transactions/{transaction!.Id}");

        // Assert — transação não deve ser encontrada após soft delete
        var getResponse = await _client.GetAsync(
            $"/api/transactions/{transaction.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helpers

    private async Task<CategoryDto?> CreateCategoryAsync() =>
        await (await _client.PostAsJsonAsync("/api/categories", ValidCategoryRequest))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private async Task<TransactionDto?> CreateTransactionAsync(
        Guid categoryId,
        decimal amount) =>
        await (await _client.PostAsJsonAsync("/api/transactions",
            new CreateTransactionRequestDto(
                Amount: amount,
                Type: TransactionType.Expense,
                Date: DateTime.UtcNow,
                Description: "Transação de teste",
                Status: TransactionStatus.Paid,
                IsRecurring: false,
                RecurrenceType: RecurrenceType.None,
                CategoryId: categoryId,
                SubcategoryId: null,
                Tags: [])))
            .Content.ReadFromJsonAsync<TransactionDto>();
}
