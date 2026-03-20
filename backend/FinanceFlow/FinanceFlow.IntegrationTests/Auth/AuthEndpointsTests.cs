using System.Net;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Auth;

public class AuthEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
        Name: "Aurel Integration",
        Email: "integration@teste.com",
        Password: "Teste@123",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo"
    );

    [Fact]
    public async Task Register_DeveRetornar201_QuandoDadosSaoValidos()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", ValidRegisterRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(ValidRegisterRequest.Email);
    }

    [Fact]
    public async Task Register_DeveRetornar422_QuandoDadosSaoInvalidos()
    {
        // Arrange
        var request = new RegisterRequestDto(
            Name: "A",
            Email: "email-invalido",
            Password: "fraca",
            Currency: null,
            Timezone: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_DeveRetornar200_QuandoCredenciaisSaoValidas()
    {
        // Arrange — registar primeiro
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest with
        {
            Email = "login@teste.com"
        });

        var loginRequest = new LoginRequestDto(
            Email: "login@teste.com",
            Password: "Teste@123"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_DeveRetornar401_QuandoPasswordErrada()
    {
        // Arrange
        var loginRequest = new LoginRequestDto(
            Email: "naoexiste@teste.com",
            Password: "SenhaErrada@123"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_DeveRetornar401_QuandoSemToken()
    {
        // Act
        var response = await _client.GetAsync("/api/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_DeveRetornar200_QuandoAutenticado()
    {
        // Arrange — registar e fazer login
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest with
        {
            Email = "profile@teste.com"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto("profile@teste.com", "Teste@123"));

        var authResult = await loginResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", authResult!.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("profile@teste.com");
    }
}
