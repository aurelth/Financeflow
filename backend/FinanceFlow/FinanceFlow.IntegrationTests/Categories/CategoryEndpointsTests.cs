using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Categories;

public class CategoryEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
    Name: "Aurel Integration",
    Email: "categorias@teste.com",
    Password: "Teste@123",
    Cpf: TestCpfGenerator.Next(),
    Gender: "Male",
    Currency: "BRL",
    Timezone: "America/Sao_Paulo");

    private static readonly CreateCategoryRequestDto ValidCategoryRequest = new(
        Name: "Jogos Online",
        Icon: "🎮",
        Color: "#6366f1",
        Type: TransactionType.Expense
    );

    // Registra e autentica, retornando o client com o token configurado
    private async Task AuthenticateAsync(string email = "categorias@teste.com")
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            ValidRegisterRequest with
            {
                Email = email,
                Cpf = TestCpfGenerator.Next() // CPF único por teste
            });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    // GET /api/categories

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("getall@teste.com");

        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<CategoryDto>>();
        result.Should().NotBeNull();
        // Deve conter ao menos as categorias padrão do sistema
        result.Should().NotBeEmpty();
    }

    // GET /api/categories/{id}

    [Fact]
    public async Task GetById_DeveRetornar200_QuandoCategoriaExiste()
    {
        // Arrange
        await AuthenticateAsync("getbyid@teste.com");

        var created = await CreateCategoryAsync();

        // Act
        var response = await _client.GetAsync($"/api/categories/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be(ValidCategoryRequest.Name);
        result.IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_DeveRetornar404_QuandoCategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("getbyid404@teste.com");

        // Act
        var response = await _client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST /api/categories

    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("create@teste.com");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/categories", ValidCategoryRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(ValidCategoryRequest.Name);
        result.Icon.Should().Be(ValidCategoryRequest.Icon);
        result.Color.Should().Be(ValidCategoryRequest.Color);
        result.IsOwner.Should().BeTrue();
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoDadosSaoInvalidos()
    {
        // Arrange
        await AuthenticateAsync("create422@teste.com");

        var request = new CreateCategoryRequestDto(
            Name: "A",          // mínimo 2 caracteres
            Icon: "",           // obrigatório
            Color: "invalida",  // formato inválido
            Type: TransactionType.Expense
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoNomeJaExiste()
    {
        // Arrange
        await AuthenticateAsync("createduplicate@teste.com");

        await _client.PostAsJsonAsync("/api/categories", ValidCategoryRequest);

        // Act — tenta criar novamente com o mesmo nome e tipo
        var response = await _client.PostAsJsonAsync(
            "/api/categories", ValidCategoryRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT /api/categories/{id}

    [Fact]
    public async Task Update_DeveRetornar200_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("update@teste.com");

        var created = await CreateCategoryAsync();

        var updateRequest = new UpdateCategoryRequestDto(
            Name: "Lazer Atualizado",
            Icon: "🎯",
            Color: "#22c55e"
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Lazer Atualizado");
        result.Icon.Should().Be("🎯");
        result.Color.Should().Be("#22c55e");
    }

    [Fact]
    public async Task Update_DeveRetornar404_QuandoCategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("update404@teste.com");

        var updateRequest = new UpdateCategoryRequestDto(
            Name: "Qualquer",
            Icon: "🎯",
            Color: "#22c55e"
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_DeveRetornar422_QuandoCategoriaEhPadrao()
    {
        // Arrange
        await AuthenticateAsync("updatepadrao@teste.com");

        // Busca uma categoria padrão do sistema
        var categoriesResponse = await _client.GetAsync("/api/categories");
        var categories = await categoriesResponse.Content
            .ReadFromJsonAsync<IEnumerable<CategoryDto>>();

        var defaultCategory = categories!.First(c => c.IsDefault);

        var updateRequest = new UpdateCategoryRequestDto(
            Name: "Tentativa",
            Icon: "❌",
            Color: "#ff0000"
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{defaultCategory.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // DELETE /api/categories/{id}

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoCategoriaExiste()
    {
        // Arrange
        await AuthenticateAsync("delete@teste.com");

        var created = await CreateCategoryAsync();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/categories/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoCategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("delete404@teste.com");

        // Act
        var response = await _client.DeleteAsync(
            $"/api/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveRetornar422_QuandoCategoriaEhPadrao()
    {
        // Arrange
        await AuthenticateAsync("deletepadrao@teste.com");

        var categoriesResponse = await _client.GetAsync("/api/categories");
        var categories = await categoriesResponse.Content
            .ReadFromJsonAsync<IEnumerable<CategoryDto>>();

        var defaultCategory = categories!.First(c => c.IsDefault);

        // Act
        var response = await _client.DeleteAsync(
            $"/api/categories/{defaultCategory.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // GET /api/categories/{categoryId}/subcategories

    [Fact]
    public async Task GetSubcategories_DeveRetornar200_QuandoCategoriaExiste()
    {
        // Arrange
        await AuthenticateAsync("getsubctg@teste.com");

        var category = await CreateCategoryAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/categories/{category!.Id}/subcategories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<SubcategoryDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubcategories_DeveRetornar404_QuandoCategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("getsubctg404@teste.com");

        // Act
        var response = await _client.GetAsync(
            $"/api/categories/{Guid.NewGuid()}/subcategories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST /api/categories/{categoryId}/subcategories

    [Fact]
    public async Task CreateSubcategory_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("createsubctg@teste.com");

        var category = await CreateCategoryAsync();

        var request = new CreateSubcategoryRequestDto(Name: "Restaurante");

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/categories/{category!.Id}/subcategories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<SubcategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Restaurante");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSubcategory_DeveRetornar422_QuandoNomeJaExiste()
    {
        // Arrange
        await AuthenticateAsync("createsubctgdup@teste.com");

        var category = await CreateCategoryAsync();
        var request = new CreateSubcategoryRequestDto(Name: "Restaurante");

        await _client.PostAsJsonAsync(
            $"/api/categories/{category!.Id}/subcategories", request);

        // Act — tenta criar novamente com o mesmo nome
        var response = await _client.PostAsJsonAsync(
            $"/api/categories/{category.Id}/subcategories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateSubcategory_DeveRetornar422_QuandoCategoriaEhPadrao()
    {
        // Arrange
        await AuthenticateAsync("createsubctgpadrao@teste.com");

        var categoriesResponse = await _client.GetAsync("/api/categories");
        var categories = await categoriesResponse.Content
            .ReadFromJsonAsync<IEnumerable<CategoryDto>>();

        var defaultCategory = categories!.First(c => c.IsDefault);
        var request = new CreateSubcategoryRequestDto(Name: "Qualquer");

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/categories/{defaultCategory.Id}/subcategories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT /api/categories/{categoryId}/subcategories/{subcategoryId}

    [Fact]
    public async Task UpdateSubcategory_DeveRetornar200_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("updatesubctg@teste.com");

        var category = await CreateCategoryAsync();
        var subcategory = await CreateSubcategoryAsync(category!.Id, "Restaurante");

        var request = new UpdateSubcategoryRequestDto(Name: "Mercado");

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{category.Id}/subcategories/{subcategory!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SubcategoryDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Mercado");
    }

    [Fact]
    public async Task UpdateSubcategory_DeveRetornar404_QuandoSubcategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("updatesubctg404@teste.com");

        var category = await CreateCategoryAsync();
        var request = new UpdateSubcategoryRequestDto(Name: "Mercado");

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{category!.Id}/subcategories/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DELETE /api/categories/{categoryId}/subcategories/{subcategoryId}

    [Fact]
    public async Task DeleteSubcategory_DeveRetornar204_QuandoSubcategoriaExiste()
    {
        // Arrange
        await AuthenticateAsync("deletesubctg@teste.com");

        var category = await CreateCategoryAsync();
        var subcategory = await CreateSubcategoryAsync(category!.Id, "Restaurante");

        // Act
        var response = await _client.DeleteAsync(
            $"/api/categories/{category.Id}/subcategories/{subcategory!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteSubcategory_DeveRetornar404_QuandoSubcategoriaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("deletesubctg404@teste.com");

        var category = await CreateCategoryAsync();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/categories/{category!.Id}/subcategories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helpers

    private async Task<CategoryDto?> CreateCategoryAsync() =>
        await (await _client.PostAsJsonAsync("/api/categories", ValidCategoryRequest))
            .Content.ReadFromJsonAsync<CategoryDto>();

    private async Task<SubcategoryDto?> CreateSubcategoryAsync(
        Guid categoryId, string name) =>
        await (await _client.PostAsJsonAsync(
            $"/api/categories/{categoryId}/subcategories",
            new CreateSubcategoryRequestDto(Name: name)))
            .Content.ReadFromJsonAsync<SubcategoryDto>();
}
