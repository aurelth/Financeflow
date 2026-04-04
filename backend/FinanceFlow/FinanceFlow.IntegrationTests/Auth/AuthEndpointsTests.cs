using System.Net;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Auth;

public class AuthEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static RegisterRequestDto BuildRegisterRequest(
        string email, string? cpf = null) => new(
        Name: "Aurel Integration",
        Email: email,
        Password: "Teste@123",
        Cpf: cpf ?? TestCpfGenerator.Next(),
        Gender: "Male",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo");

    [Fact]
    public async Task Register_DeveRetornar201_QuandoDadosSaoValidos()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", BuildRegisterRequest("integration@teste.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("integration@teste.com");
    }

    [Fact]
    public async Task Register_DeveRetornar422_QuandoDadosSaoInvalidos()
    {
        var request = new RegisterRequestDto(
            Name: "A",
            Email: "email-invalido",
            Password: "fraca",
            Cpf: "",
            Gender: "Male",
            Currency: null,
            Timezone: null);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_DeveRetornar200_QuandoCredenciaisSaoValidas()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            BuildRegisterRequest("login@teste.com"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("login@teste.com", "Teste@123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_DeveRetornar401_QuandoPasswordErrada()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("naoexiste@teste.com", "SenhaErrada@123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_DeveRetornar401_QuandoSemToken()
    {
        var response = await _client.GetAsync("/api/users/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_DeveRetornar200_QuandoAutenticado()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            BuildRegisterRequest("profile@teste.com"));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("profile@teste.com", "Teste@123"));

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", authResult!.AccessToken);

        var response = await _client.GetAsync("/api/users/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("profile@teste.com");
    }
}
