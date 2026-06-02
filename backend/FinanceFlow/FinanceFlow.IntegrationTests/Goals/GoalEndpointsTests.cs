using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Goals;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanceFlow.IntegrationTests.Goals;

public class GoalEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
        Name: "Aurel Goals",
        Email: "goals@teste.com",
        Password: "Teste@123",
        Cpf: TestCpfGenerator.Next(),
        Gender: "Male",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo");

    private async Task AuthenticateAsync(string email = "goals@teste.com")
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            ValidRegisterRequest with
            {
                Email = email,
                Cpf = TestCpfGenerator.Next()
            });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadAsJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    private static CreateGoalRequestDto ValidGoalRequest => new(
        Name: "Viagem para Europa",
        TargetAmount: 10000,
        MonthlyContribution: 500,
        Deadline: DateTime.UtcNow.AddMonths(24),
        Emoji: "✈️");

    // GET /api/goals

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.GetAsync("/api/goals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange
        await AuthenticateAsync("goals.getall@teste.com");

        // Act
        var response = await _client.GetAsync("/api/goals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<GoalsSummaryResultDto>();
        result.Should().NotBeNull();
        result!.Goals.Should().NotBeNull();
    }

    // POST /api/goals

    [Fact]
    public async Task Create_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("goals.create@teste.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadAsJsonAsync<GoalProgressResultDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Viagem para Europa");
        result.TargetAmount.Should().Be(10000);
        result.MonthlyContribution.Should().Be(500);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoValorAlvoZero()
    {
        // Arrange
        await AuthenticateAsync("goals.create422@teste.com");

        var request = ValidGoalRequest with { TargetAmount = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoPrazoNoPassado()
    {
        // Arrange
        await AuthenticateAsync("goals.createprazo@teste.com");

        var request = ValidGoalRequest with { Deadline = DateTime.UtcNow.AddDays(-1) };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoContribuicaoMaiorQueAlvo()
    {
        // Arrange
        await AuthenticateAsync("goals.createcontrib@teste.com");

        var request = ValidGoalRequest with
        {
            TargetAmount = 100,
            MonthlyContribution = 500,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT /api/goals/{id}

    [Fact]
    public async Task Update_DeveRetornar200_QuandoDadosSaoValidos()
    {
        // Arrange
        await AuthenticateAsync("goals.update@teste.com");

        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        var updateRequest = new UpdateGoalRequestDto(
            Name: "Viagem para Japão",
            TargetAmount: 15000,
            MonthlyContribution: 700,
            Deadline: DateTime.UtcNow.AddMonths(24),
            Emoji: "🗾");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/goals/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<GoalProgressResultDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Viagem para Japão");
        result.TargetAmount.Should().Be(15000);
    }

    [Fact]
    public async Task Update_DeveRetornar404_QuandoMetaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("goals.update404@teste.com");

        var request = new UpdateGoalRequestDto(
            Name: "Qualquer",
            TargetAmount: 5000,
            MonthlyContribution: 400,
            Deadline: DateTime.UtcNow.AddMonths(12),
            Emoji: "🎯");

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/goals/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DELETE /api/goals/{id}

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoMetaExiste()
    {
        // Arrange
        await AuthenticateAsync("goals.delete@teste.com");

        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/goals/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoMetaNaoExiste()
    {
        // Arrange
        await AuthenticateAsync("goals.delete404@teste.com");

        // Act
        var response = await _client.DeleteAsync($"/api/goals/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveAplicarSoftDelete_QuandoMetaExiste()
    {
        // Arrange
        await AuthenticateAsync("goals.softdelete@teste.com");

        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        // Act
        await _client.DeleteAsync($"/api/goals/{created!.Id}");

        // Assert — meta não deve aparecer na listagem após soft delete
        var getResponse = await _client.GetAsync("/api/goals");
        var result = await getResponse.Content.ReadAsJsonAsync<GoalsSummaryResultDto>();
        result!.Goals.Should().NotContain(g => g.Id == created.Id);
    }

    // Testes de categoria vinculada

    [Fact]
    public async Task Create_DeveCriarCategoriaVinculada_QuandoMetaCriada()
    {
        // Arrange
        await AuthenticateAsync("goals.createcat@teste.com");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        // Assert — meta deve ter LinkedCategoryId preenchido
        created.Should().NotBeNull();
        created!.LinkedCategoryId.Should().NotBeNull();

        // Assert — categoria deve aparecer no endpoint de categorias de metas
        var categoriesResponse = await _client.GetAsync("/api/categories/goals");
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await categoriesResponse.Content.ReadAsJsonAsync<IEnumerable<CategoryDto>>();
        categories.Should().NotBeNull();
        categories!.Should().Contain(c =>
            c.Id == created.LinkedCategoryId &&
            c.Name == $"Meta: {ValidGoalRequest.Name}");
    }

    [Fact]
    public async Task Update_DeveRenomearCategoriaVinculada_QuandoNomeMudou()
    {
        // Arrange
        await AuthenticateAsync("goals.updatecat@teste.com");

        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        var updateRequest = new UpdateGoalRequestDto(
            Name: "Viagem para Japão",
            TargetAmount: 15000,
            MonthlyContribution: 700,
            Deadline: DateTime.UtcNow.AddMonths(24),
            Emoji: "🗾");

        // Act
        await _client.PutAsJsonAsync($"/api/goals/{created!.Id}", updateRequest);

        // Assert — categoria deve ter o novo nome
        var categoriesResponse = await _client.GetAsync("/api/categories/goals");
        var categories = await categoriesResponse.Content.ReadAsJsonAsync<IEnumerable<CategoryDto>>();
        categories!.Should().Contain(c =>
            c.Id == created.LinkedCategoryId &&
            c.Name == "Meta: Viagem para Japão");
    }

    [Fact]
    public async Task Delete_DeveArquivarCategoriaVinculada_QuandoMetaExcluida()
    {
        // Arrange
        await AuthenticateAsync("goals.deletecat@teste.com");

        var createResponse = await _client.PostAsJsonAsync("/api/goals", ValidGoalRequest);
        var created = await createResponse.Content.ReadAsJsonAsync<GoalProgressResultDto>();

        // Act
        await _client.DeleteAsync($"/api/goals/{created!.Id}");

        // Assert — categoria arquivada não deve aparecer no endpoint de categorias de metas
        var categoriesResponse = await _client.GetAsync("/api/categories/goals");
        var categories = await categoriesResponse.Content.ReadAsJsonAsync<IEnumerable<CategoryDto>>();
        categories!.Should().NotContain(c => c.Id == created.LinkedCategoryId);
    }
}
