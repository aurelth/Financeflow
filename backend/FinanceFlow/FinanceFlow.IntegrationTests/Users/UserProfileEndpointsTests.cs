using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Users;

public class UserProfileEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task<(HttpClient client, UserProfileDto profile)> AuthenticateAsync(
        HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "User Profile Teste",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        var auth = await loginResponse.Content.ReadAsJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileResponse = await client.GetAsync("/api/users/profile");
        var profile = await profileResponse.Content.ReadAsJsonAsync<UserProfileDto>();

        return (client, profile!);
    }

    // GET /api/users/profile

    [Fact]
    public async Task GetProfile_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/users/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_DeveRetornar200_QuandoAutenticado()
    {
        var client = CreateClient();
        var (_, profile) = await AuthenticateAsync(client, "getprofile@teste.com");

        profile.Should().NotBeNull();
        profile.Email.Should().Be("getprofile@teste.com");
        profile.Currency.Should().Be("BRL");
        profile.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task GetProfile_DeveRetornarLanguagePadrao_QuandoNaoDefinido()
    {
        var client = CreateClient();
        var (_, profile) = await AuthenticateAsync(client, "getprofile.lang@teste.com");

        // Language deve ter valor padrão pt-BR
        profile.Language.Should().Be("pt-BR");
    }

    // PUT /api/users/profile

    [Fact]
    public async Task UpdateProfile_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequestDto("USD", "America/New_York", "en-US"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_DeveRetornar200_QuandoDadosSaoValidos()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "updateprofile@teste.com");

        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequestDto("USD", "America/New_York", "en-US"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<UserProfileDto>();
        result.Should().NotBeNull();
        result!.Currency.Should().Be("USD");
        result.Timezone.Should().Be("America/New_York");
        result.Language.Should().Be("en-US");
    }

    [Fact]
    public async Task UpdateProfile_DeveActualizarLanguage_QuandoIdiomaValido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "updatelang@teste.com");

        foreach (var lang in new[] { "en-US", "es-ES", "fr-FR", "pt-BR" })
        {
            var response = await client.PutAsJsonAsync("/api/users/profile",
                new UpdateProfileRequestDto("BRL", "America/Sao_Paulo", lang));

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadAsJsonAsync<UserProfileDto>();
            result!.Language.Should().Be(lang);
        }
    }

    [Fact]
    public async Task UpdateProfile_DeveRetornar422_QuandoLanguageInvalido()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "updatelang.invalid@teste.com");

        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequestDto("BRL", "America/Sao_Paulo", "de-DE"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProfile_DeveRetornar422_QuandoCurrencyVazia()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "updateprofile.currency@teste.com");

        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequestDto("", "America/Sao_Paulo", "pt-BR"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProfile_DeveRetornar422_QuandoTimezoneVazio()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "updateprofile.timezone@teste.com");

        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequestDto("BRL", "", "pt-BR"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
