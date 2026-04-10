using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Admin;

public class AdminEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    // Usa as credenciais do AdminSeed configuradas na factory
    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("admin@financeflow.com", "Admin@123456"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    private static async Task<Guid> CreateUserAndGetIdAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequestDto(
                Name: "Usuário Teste",
                Email: email,
                Password: "Teste@123",
                Cpf: TestCpfGenerator.Next(),
                Gender: "Male",
                Currency: "BRL",
                Timezone: "America/Sao_Paulo"));

        var user = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        return user!.Id;
    }

    // GET /api/admin/users

    [Fact]
    public async Task GetUsers_DeveRetornar200_QuandoAdmin()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AdminUserListDto>();
        result.Should().NotBeNull();
        result!.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUsers_DeveRetornar403_QuandoUsuarioComum()
    {
        // Arrange
        var client = CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "User Comum", Email: "admin.getusers.common@teste.com",
            Password: "Teste@123", Cpf: TestCpfGenerator.Next(),
            Gender: "Male", Currency: "BRL", Timezone: "America/Sao_Paulo"));

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("admin.getusers.common@teste.com", "Teste@123"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_DeveRetornar401_QuandoNaoAutenticado()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // PATCH /api/admin/users/{id}/deactivate

    [Fact]
    public async Task DeactivateUser_DeveRetornar204_QuandoValido()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);
        var userId = await CreateUserAndGetIdAsync(client, "admin.deactivate@teste.com");

        // Act
        var response = await client.PatchAsync(
            $"/api/admin/users/{userId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivateUser_DeveRetornar422_QuandoUnicoAdmin()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);

        var usersResponse = await client.GetAsync("/api/admin/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<AdminUserListDto>();
        var admin = users!.Users.First(u => u.Role == "Admin");

        // Act
        var response = await client.PatchAsync(
            $"/api/admin/users/{admin.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PATCH /api/admin/users/{id}/reactivate

    [Fact]
    public async Task ReactivateUser_DeveRetornar204_QuandoValido()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);
        var userId = await CreateUserAndGetIdAsync(client, "admin.reactivate@teste.com");

        await client.PatchAsync($"/api/admin/users/{userId}/deactivate", null);

        // Act
        var response = await client.PatchAsync(
            $"/api/admin/users/{userId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // PATCH /api/admin/users/{id}/promote

    [Fact]
    public async Task PromoteUser_DeveRetornar204_QuandoValido()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);
        var userId = await CreateUserAndGetIdAsync(client, "admin.promote@teste.com");

        // Act
        var response = await client.PatchAsync(
            $"/api/admin/users/{userId}/promote", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // PATCH /api/admin/users/{id}/demote

    [Fact]
    public async Task DemoteUser_DeveRetornar204_QuandoValido()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);
        var userId = await CreateUserAndGetIdAsync(client, "admin.demote@teste.com");

        await client.PatchAsync($"/api/admin/users/{userId}/promote", null);

        // Act
        var response = await client.PatchAsync(
            $"/api/admin/users/{userId}/demote", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // GET /api/admin/categories

    [Fact]
    public async Task GetDefaultCategories_DeveRetornar200_ComCategorias()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);

        // Act
        var response = await client.GetAsync("/api/admin/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<AdminCategoryDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThan(0);
    }

    // POST /api/admin/categories

    [Fact]
    public async Task CreateDefaultCategory_DeveRetornar201_QuandoValido()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);

        var body = new CreateDefaultCategoryRequestDto(
            Name: "Categoria Admin Teste",
            Icon: "star",
            Color: "#10b981",
            Type: TransactionType.Expense);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/categories", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AdminCategoryDto>();
        result!.Name.Should().Be("Categoria Admin Teste");
    }

    // GET /api/admin/metrics

    [Fact]
    public async Task GetMetrics_DeveRetornar200_ComDados()
    {
        // Arrange
        var client = CreateClient();
        await AuthenticateAsAdminAsync(client);

        // Act
        var response = await client.GetAsync("/api/admin/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AdminMetricsDto>();
        result.Should().NotBeNull();
        result!.TotalUsers.Should().BeGreaterThan(0);
        result.DefaultCategories.Should().BeGreaterThan(0);
    }
}
