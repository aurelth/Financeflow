using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Assistant;
using FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanceFlow.IntegrationTests.Assistant;

public class AssistantEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly RegisterRequestDto ValidRegisterRequest = new(
        Name: "Aurel Assistant",
        Email: "assistant@teste.com",
        Password: "Teste@123",
        Cpf: TestCpfGenerator.Next(),
        Gender: "Male",
        Currency: "BRL",
        Timezone: "America/Sao_Paulo");

    private async Task AuthenticateAsync(string email = "assistant@teste.com")
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

    // POST /api/assistant/chat

    [Fact]
    public async Task Chat_DeveRetornar401_QuandoSemToken()
    {
        // Arrange
        var request = new SendMessageRequestDto("Quanto gastei este mês?");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assistant/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Chat_DeveRetornar200_QuandoMensagemValida()
    {
        // Arrange
        await AuthenticateAsync("chat.valido@teste.com");

        var request = new SendMessageRequestDto("Quanto gastei este mês?");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assistant/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<SendMessageResponse>();
        result.Should().NotBeNull();
        result!.Reply.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Chat_DeveRetornar422_QuandoMensagemVazia()
    {
        // Arrange
        await AuthenticateAsync("chat.vazio@teste.com");

        var request = new SendMessageRequestDto(string.Empty);

        // Act
        var response = await _client.PostAsJsonAsync("/api/assistant/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
