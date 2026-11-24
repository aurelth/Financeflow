using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.QuickEntry;
using FinanceFlow.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace FinanceFlow.IntegrationTests.QuickEntry;

/// <summary>
/// Lançamento por IA. A "IA" é o AiReplyStub da fábrica (nunca a Anthropic de verdade);
/// o limite diário dos testes é 3 (Ai:DailyLimit na fábrica).
/// </summary>
public class QuickEntryEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private const int DailyLimit = 3;

    private AiReplyStub Stub => factory.Services.GetRequiredService<AiReplyStub>();

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Lançamento IA",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, "Teste@123"));
        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadAsJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequestDto(name, "🍽️", "#f59e0b", TransactionType.Expense));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<CategoryDto>())!;
    }

    private static string Yesterday => DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

    private static string ReplyWith(Guid? categoryId, decimal amount = 45.9m, string description = "Almoço iFood") =>
        JsonSerializer.Serialize(new
        {
            entries = new[]
            {
                new
                {
                    type = "Expense",
                    amount,
                    date = Yesterday,
                    description,
                    categoryId = categoryId?.ToString(),
                },
            },
        });

    private static MultipartFormDataContent TextForm(string text) => new() { { new StringContent(text), "text" } };

    private static byte[] PngBytes()
    {
        using var bitmap = new SKBitmap(80, 120);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static MultipartFormDataContent ImageForm(byte[] bytes, string fileName = "recibo.png", string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "image", fileName } };
    }

    private static async Task<AiQuotaDto> GetQuotaAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/quickentry/quota");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadAsJsonAsync<AiQuotaDto>())!;
    }

    private void ResetStub(string? reply = null)
    {
        Stub.Failure = null;
        Stub.QuickEntryReply = reply ?? "{\"entries\":[]}";
        Stub.Calls = 0;
    }

    // Autenticação

    [Fact]
    public async Task Parse_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().PostAsync("/api/quickentry/parse", TextForm("café 5"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Quota_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/quickentry/quota");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // GET /api/quickentry/quota

    [Fact]
    public async Task Quota_DeveComecarCheia()
    {
        var client = await AuthenticatedClientAsync("ia.quota.inicio@teste.com");

        var quota = await GetQuotaAsync(client);

        (quota.Limit, quota.Used, quota.Remaining).Should().Be((DailyLimit, 0, DailyLimit));
        quota.ResetsAtUtc.Should().BeAfter(DateTime.UtcNow);
        quota.ResetsAtUtc.Should().BeBefore(DateTime.UtcNow.AddHours(25));
    }

    // POST /api/quickentry/parse

    [Fact]
    public async Task Parse_DeveDevolverRascunhoComCategoria_EConsumirUmaChamada()
    {
        var client = await AuthenticatedClientAsync("ia.parse.texto@teste.com");
        var category = await CreateCategoryAsync(client, "Restaurantes IA");
        ResetStub(ReplyWith(category.Id));

        var response = await client.PostAsync("/api/quickentry/parse", TextForm("almoço 45,90 no ifood ontem"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadAsJsonAsync<QuickEntryResultDto>())!;

        var draft = result.Drafts.Should().ContainSingle().Subject;
        draft.Type.Should().Be(TransactionType.Expense);
        draft.Amount.Should().Be(45.9m);
        draft.Description.Should().Be("Almoço iFood");
        draft.CategoryId.Should().Be(category.Id);
        draft.CategoryName.Should().Be("Restaurantes IA");
        draft.Date.Date.Should().Be(DateTime.UtcNow.AddDays(-1).Date);

        result.Quota.Used.Should().Be(1);
        result.Quota.Remaining.Should().Be(DailyLimit - 1);
        (await GetQuotaAsync(client)).Used.Should().Be(1);
    }

    [Fact]
    public async Task Parse_DeveIgnorarCategoriaDeOutroUsuario()
    {
        var owner = await AuthenticatedClientAsync("ia.cat.dono@teste.com");
        var other = await AuthenticatedClientAsync("ia.cat.outro@teste.com");
        var foreign = await CreateCategoryAsync(owner, "Só do dono IA");
        ResetStub(ReplyWith(foreign.Id));

        var response = await other.PostAsync("/api/quickentry/parse", TextForm("almoço 45"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadAsJsonAsync<QuickEntryResultDto>())!;
        result.Drafts.Single().CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Parse_DeveAceitarFotoDeComprovante()
    {
        var client = await AuthenticatedClientAsync("ia.parse.foto@teste.com");
        ResetStub(ReplyWith(null, 89.5m, "Posto Shell"));

        var response = await client.PostAsync("/api/quickentry/parse", ImageForm(PngBytes()));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadAsJsonAsync<QuickEntryResultDto>())!;
        result.Drafts.Single().Description.Should().Be("Posto Shell");
        Stub.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Parse_DeveRetornar422_QuandoOTextoEstaEmBrancoESemImagem()
    {
        var client = await AuthenticatedClientAsync("ia.parse.vazio@teste.com");
        ResetStub();

        var response = await client.PostAsync("/api/quickentry/parse", TextForm("   "));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        Stub.Calls.Should().Be(0);
        (await GetQuotaAsync(client)).Used.Should().Be(0);
    }

    [Fact]
    public async Task Parse_DeveRetornar400_ENaoChamarAIA_QuandoOMultipartVemSemNenhumaParte()
    {
        // Corpo malformado: o ASP.NET recusa antes de chegar ao validador (não é erro de regra de negócio)
        var client = await AuthenticatedClientAsync("ia.parse.semcorpo@teste.com");
        ResetStub();

        var response = await client.PostAsync("/api/quickentry/parse", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Stub.Calls.Should().Be(0);
        (await GetQuotaAsync(client)).Used.Should().Be(0);
    }

    [Fact]
    public async Task Parse_DeveRetornar422_ENaoGastarCota_QuandoOArquivoNaoEImagem()
    {
        var client = await AuthenticatedClientAsync("ia.parse.falsa@teste.com");
        ResetStub();

        // Content-Type diz image/jpeg, o conteúdo é texto: o servidor não pode confiar no cabeçalho
        var response = await client.PostAsync("/api/quickentry/parse",
            ImageForm("sou um texto disfarçado"u8.ToArray(), "recibo.jpg", "image/jpeg"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        Stub.Calls.Should().Be(0);
        (await GetQuotaAsync(client)).Used.Should().Be(0);
    }

    [Fact]
    public async Task Parse_DeveRetornar422_QuandoOTextoEGrandeDemais()
    {
        var client = await AuthenticatedClientAsync("ia.parse.longo@teste.com");
        ResetStub();

        var response = await client.PostAsync("/api/quickentry/parse", TextForm(new string('a', 501)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        Stub.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Parse_DeveDevolverACota_QuandoARespostaDaIAEIlegivel()
    {
        var client = await AuthenticatedClientAsync("ia.parse.ilegivel@teste.com");
        ResetStub("desculpe, não consegui ler");

        var response = await client.PostAsync("/api/quickentry/parse", TextForm("café 5"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await GetQuotaAsync(client)).Used.Should().Be(0);
    }

    [Fact]
    public async Task Parse_DeveDevolverACota_QuandoAAnthropicFalha()
    {
        var client = await AuthenticatedClientAsync("ia.parse.falha@teste.com");
        ResetStub();
        Stub.Failure = new HttpRequestException("anthropic fora do ar");

        var response = await client.PostAsync("/api/quickentry/parse", TextForm("café 5"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await GetQuotaAsync(client)).Used.Should().Be(0);

        ResetStub();
    }

    // Limite diário

    [Fact]
    public async Task Parse_DeveRetornar429_QuandoACotaDiariaAcaba()
    {
        var client = await AuthenticatedClientAsync("ia.limite@teste.com");
        ResetStub(ReplyWith(null));

        for (var i = 0; i < DailyLimit; i++)
        {
            var ok = await client.PostAsync("/api/quickentry/parse", TextForm($"café {i + 1}"));
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var blocked = await client.PostAsync("/api/quickentry/parse", TextForm("mais um café"));

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.RetryAfter.Should().NotBeNull();
        var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("limit").GetInt32().Should().Be(DailyLimit);
        body.GetProperty("message").GetString().Should().Contain(DailyLimit.ToString());

        // A IA não foi chamada na tentativa bloqueada
        Stub.Calls.Should().Be(DailyLimit);

        var quota = await GetQuotaAsync(client);
        (quota.Used, quota.Remaining).Should().Be((DailyLimit, 0));
    }

    [Fact]
    public async Task Parse_NuncaDevePassarDoLimite_ComRequisicoesSimultaneas()
    {
        var client = await AuthenticatedClientAsync("ia.concorrencia@teste.com");
        ResetStub(ReplyWith(null));

        // 10 requisições ao mesmo tempo para uma cota de 3: o UPDATE atômico tem de deixar passar exatamente 3
        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(i => client.PostAsync("/api/quickentry/parse", TextForm($"café {i}"))));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(DailyLimit);
        responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(10 - DailyLimit);
        Stub.Calls.Should().Be(DailyLimit);
        (await GetQuotaAsync(client)).Used.Should().Be(DailyLimit);
    }

    [Fact]
    public async Task Cota_DeveSerIndependentePorUsuario()
    {
        var heavy = await AuthenticatedClientAsync("ia.cota.pesado@teste.com");
        var light = await AuthenticatedClientAsync("ia.cota.leve@teste.com");
        ResetStub(ReplyWith(null));

        for (var i = 0; i < DailyLimit; i++)
            (await heavy.PostAsync("/api/quickentry/parse", TextForm($"café {i}"))).EnsureSuccessStatusCode();

        (await GetQuotaAsync(heavy)).Remaining.Should().Be(0);
        (await GetQuotaAsync(light)).Remaining.Should().Be(DailyLimit);
        (await light.PostAsync("/api/quickentry/parse", TextForm("café"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
