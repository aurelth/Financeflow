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

    // Cria multipart/form-data para o endpoint POST /api/transactions
    private static MultipartFormDataContent BuildTransactionForm(
        decimal amount,
        TransactionType type,
        DateTime date,
        string description,
        TransactionStatus status,
        bool isRecurring,
        RecurrenceType recurrenceType,
        Guid categoryId,
        Guid? subcategoryId,
        string[] tags)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)), "amount" },
            { new StringContent(((int)type).ToString()),           "type"           },
            { new StringContent(date.ToString("o")),               "date"           },
            { new StringContent(description),                      "description"    },
            { new StringContent(((int)status).ToString()),         "status"         },
            { new StringContent(isRecurring.ToString().ToLower()), "isRecurring"    },
            { new StringContent(((int)recurrenceType).ToString()), "recurrenceType" },
            { new StringContent(categoryId.ToString()),            "categoryId"     },
        };

        if (subcategoryId.HasValue)
            form.Add(new StringContent(subcategoryId.Value.ToString()), "subcategoryId");

        foreach (var tag in tags)
            form.Add(new StringContent(tag), "tags");

        return form;
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

        var form = BuildTransactionForm(
            amount: 75.00m,
            type: TransactionType.Expense,
            date: DateTime.UtcNow,
            description: "Cinema",
            status: TransactionStatus.Paid,
            isRecurring: false,
            recurrenceType: RecurrenceType.None,
            categoryId: category!.Id,
            subcategoryId: null,
            tags: ["lazer"]);

        // Act
        var response = await _client.PostAsync("/api/transactions", form);

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

        var form = BuildTransactionForm(
            amount: -10.00m,       // valor inválido
            type: TransactionType.Expense,
            date: DateTime.UtcNow,
            description: "Inválido",
            status: TransactionStatus.Paid,
            isRecurring: false,
            recurrenceType: RecurrenceType.None,
            categoryId: Guid.Empty,    // categoria inválida
            subcategoryId: null,
            tags: []);

        // Act
        var response = await _client.PostAsync("/api/transactions", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoTipoNaoCoincideComCategoria()
    {
        // Arrange
        await AuthenticateAsync("create.tipo.tx@teste.com");

        var category = await CreateCategoryAsync(); // Type = Expense

        var form = BuildTransactionForm(
            amount: 100.00m,
            type: TransactionType.Income, // tipo errado
            date: DateTime.UtcNow,
            description: "Tipo errado",
            status: TransactionStatus.Paid,
            isRecurring: false,
            recurrenceType: RecurrenceType.None,
            categoryId: category!.Id,
            subcategoryId: null,
            tags: []);

        // Act
        var response = await _client.PostAsync("/api/transactions", form);

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

    // GET /api/transactions/{id}/attachment

    [Fact]
    public async Task GetAttachment_DeveRetornar200_QuandoAnexoExiste()
    {
        // Arrange
        await AuthenticateAsync("getattachment.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 50.00m);

        // Faz upload de um anexo
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF });
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var uploadForm = new MultipartFormDataContent();
        uploadForm.Add(fileContent, "file", "comprovante.jpg");

        var uploadResponse = await _client.PostAsync(
            $"/api/transactions/{transaction!.Id}/attachment", uploadForm);

        // Só testa o GET se o upload foi bem-sucedido
        uploadResponse.EnsureSuccessStatusCode();

        // Verifica que o anexo foi guardado
        var updated = await _client.GetAsync($"/api/transactions/{transaction.Id}");
        var updatedTx = await updated.Content.ReadFromJsonAsync<TransactionDto>();
        updatedTx!.AttachmentPath.Should().NotBeNullOrEmpty();

        // Act
        var response = await _client.GetAsync(
            $"/api/transactions/{transaction.Id}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task GetAttachment_DeveRetornar404_QuandoTransacaoNaoTemAnexo()
    {
        // Arrange
        await AuthenticateAsync("getattachment404.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 50.00m);

        // Act — transação sem anexo
        var response = await _client.GetAsync(
            $"/api/transactions/{transaction!.Id}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAttachment_DeveRetornar404_QuandoTransacaoNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("getattachment404tx.tx@teste.com");

        // Act
        var response = await _client.GetAsync(
            $"/api/transactions/{Guid.NewGuid()}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }    

    [Fact]
    public async Task RemoveAttachment_DeveRetornar204_QuandoAnexoExiste()
    {
        // Arrange
        await AuthenticateAsync("removeattachment.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 50.00m);

        // Faz upload de um anexo primeiro
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF }); // JPEG header
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var uploadForm = new MultipartFormDataContent();
        uploadForm.Add(fileContent, "file", "comprovante.jpg");

        await _client.PostAsync(
            $"/api/transactions/{transaction!.Id}/attachment", uploadForm);

        // Act — remove o anexo
        var response = await _client.DeleteAsync(
            $"/api/transactions/{transaction.Id}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifica que o anexo foi removido
        var getResponse = await _client.GetAsync(
            $"/api/transactions/{transaction.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        result!.AttachmentPath.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAttachment_DeveRetornar404_QuandoTransacaoNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("removeattachment404.tx@teste.com");

        // Act
        var response = await _client.DeleteAsync(
            $"/api/transactions/{Guid.NewGuid()}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAttachment_DeveRetornarNomeOriginal_QuandoAnexoTemNome()
    {
        // Arrange
        await AuthenticateAsync("getattachmentname.tx@teste.com");

        var category = await CreateCategoryAsync();
        var transaction = await CreateTransactionAsync(category!.Id, 50.00m);

        // Faz upload com nome original
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF });
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var uploadForm = new MultipartFormDataContent();
        uploadForm.Add(fileContent, "file", "meu_comprovante.jpg");

        var uploadResponse = await _client.PostAsync(
            $"/api/transactions/{transaction!.Id}/attachment", uploadForm);
        uploadResponse.EnsureSuccessStatusCode();

        // Act
        var response = await _client.GetAsync(
            $"/api/transactions/{transaction.Id}/attachment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verifica que o nome original foi preservado na transação
        var getResponse = await _client.GetAsync($"/api/transactions/{transaction.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        updated!.AttachmentName.Should().Be("meu_comprovante.jpg");
    }

    // Helpers

    private async Task<CategoryDto?> CreateCategoryAsync() =>
        await (await _client.PostAsJsonAsync("/api/categories", ValidCategoryRequest))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private async Task<TransactionDto?> CreateTransactionAsync(
        Guid categoryId,
        decimal amount)
    {
        var form = BuildTransactionForm(
            amount: amount,
            type: TransactionType.Expense,
            date: DateTime.UtcNow,
            description: "Transação de teste",
            status: TransactionStatus.Paid,
            isRecurring: false,
            recurrenceType: RecurrenceType.None,
            categoryId: categoryId,
            subcategoryId: null,
            tags: []);

        var response = await _client.PostAsync("/api/transactions", form);
        return await response.Content.ReadFromJsonAsync<TransactionDto>();
    }
}
