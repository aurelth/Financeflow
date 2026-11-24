using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.DTOs.CategoryRules;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.CategoryRules;

/// <summary>Regras "descrição → categoria" aprendidas ao confirmar importações e reaplicadas nas seguintes.</summary>
public class CategoryRulesEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private sealed record Session(HttpClient Client, Guid UserId);

    private async Task<Session> AuthenticateAsync(string email)
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Regras Teste",
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
        return new Session(client, auth.User.Id);
    }

    private static async Task<AccountDto> CreateAccountAsync(Session s, string name)
    {
        var response = await s.Client.PostAsJsonAsync("/api/accounts", new CreateAccountRequestDto(
            Name: name,
            Type: AccountType.Checking,
            BankName: null,
            InitialBalance: 0m,
            InitialBalanceDate: new DateTime(2020, 1, 1),
            Currency: null,
            Color: null,
            Icon: null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<AccountDto>())!;
    }

    private static async Task<CategoryDto> CreateCategoryAsync(Session s, string name)
    {
        var response = await s.Client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequestDto(name, "🏷️", "#6366f1", TransactionType.Expense));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<CategoryDto>())!;
    }

    private static string BuildOfx(string acctId, string fitId, DateTime date, decimal amount, string memo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("OFXHEADER:100");
        sb.AppendLine("DATA:OFXSGML");
        sb.AppendLine("VERSION:102");
        sb.AppendLine("SECURITY:NONE");
        sb.AppendLine("ENCODING:USASCII");
        sb.AppendLine("CHARSET:1252");
        sb.AppendLine("COMPRESSION:NONE");
        sb.AppendLine("OLDFILEUID:NONE");
        sb.AppendLine("NEWFILEUID:NONE");
        sb.AppendLine("<OFX><SIGNONMSGSRSV1><SONRS>");
        sb.AppendLine("<STATUS><CODE>0</CODE><SEVERITY>INFO</SEVERITY></STATUS>");
        sb.AppendLine("<DTSERVER>20260301</DTSERVER><LANGUAGE>POR</LANGUAGE>");
        sb.AppendLine("</SONRS></SIGNONMSGSRSV1>");
        sb.AppendLine("<BANKMSGSRSV1><STMTTRNRS><TRNUID>1</TRNUID><STMTRS><CURDEF>BRL</CURDEF>");
        sb.AppendLine($"<BANKACCTFROM><BANKID>001</BANKID><ACCTID>{acctId}</ACCTID><ACCTTYPE>CHECKING</ACCTTYPE></BANKACCTFROM>");
        sb.AppendLine("<BANKTRANLIST><DTSTART>20260201</DTSTART><DTEND>20260228</DTEND>");
        sb.AppendLine("<STMTTRN>");
        sb.AppendLine($"<TRNTYPE>{(amount < 0 ? "DEBIT" : "CREDIT")}</TRNTYPE>");
        sb.AppendLine($"<DTPOSTED>{date:yyyyMMdd}</DTPOSTED>");
        sb.AppendLine($"<TRNAMT>{amount.ToString("F2", CultureInfo.InvariantCulture)}</TRNAMT>");
        sb.AppendLine($"<FITID>{fitId}</FITID>");
        sb.AppendLine($"<MEMO>{memo}</MEMO>");
        sb.AppendLine("</STMTTRN>");
        sb.AppendLine("</BANKTRANLIST>");
        sb.AppendLine("<LEDGERBAL><BALAMT>0.00</BALAMT><DTASOF>20260228</DTASOF></LEDGERBAL>");
        sb.AppendLine("</STMTRS></STMTTRNRS></BANKMSGSRSV1></OFX>");
        return sb.ToString();
    }

    private static async Task<BankImportPreviewDto> UploadAndProcessAsync(Session s, Guid accountId, string ofx)
    {
        var content = new MultipartFormDataContent
        {
            {
                new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(ofx)))
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                },
                "file",
                "extrato.ofx"
            },
            { new StringContent(accountId.ToString()), "accountId" },
        };

        var upload = await s.Client.PostAsync("/api/imports/ofx", content);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        var import = (await upload.Content.ReadAsJsonAsync<BankImportDto>())!;

        var process = await s.Client.PostAsJsonAsync(
            $"/api/imports/{import.Id}/process", new ProcessImportRequestDto(s.UserId));
        process.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var preview = await s.Client.GetAsync($"/api/imports/{import.Id}/preview");
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await preview.Content.ReadAsJsonAsync<BankImportPreviewDto>())!;
    }

    private static async Task ConfirmAsync(Session s, BankImportPreviewDto preview, Guid categoryId)
    {
        var items = preview.Transactions
            .Select(t => new ConfirmImportItemDto(t.Id, true, categoryId, "Expense"))
            .ToList();

        var response = await s.Client.PostAsJsonAsync(
            $"/api/imports/{preview.ImportId}/confirm", new ConfirmImportRequestDto(items));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<List<CategoryRuleDto>> ListRulesAsync(Session s)
    {
        var response = await s.Client.GetAsync("/api/categoryrules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadAsJsonAsync<List<CategoryRuleDto>>())!;
    }

    // GET /api/categoryrules

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/categoryrules");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornarListaVazia_QuandoNadaFoiAprendido()
    {
        var s = await AuthenticateAsync("regras.vazio@teste.com");

        (await ListRulesAsync(s)).Should().BeEmpty();
    }

    // Aprendizado: upload → confirmação → novo upload

    [Fact]
    public async Task Confirmar_DeveAprenderARegra_ESugeriNaProximaImportacao()
    {
        var s = await AuthenticateAsync("regras.aprende@teste.com");
        var account = await CreateAccountAsync(s, "Conta Regras");
        var category = await CreateCategoryAsync(s, "Ferragens e obras");

        // 1ª importação: nada a sugerir (descrição sem palavra-chave)
        var first = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55501", "A1", new DateTime(2026, 2, 10), -120m, "COMPRA NO DEBITO 10/02 ACME FERRAGENS 1234"));
        var firstRow = first.Transactions.Should().ContainSingle().Subject;
        firstRow.SuggestedCategoryId.Should().BeNull();
        firstRow.SuggestionSource.Should().BeNull();

        await ConfirmAsync(s, first, category.Id);

        var rules = await ListRulesAsync(s);
        var rule = rules.Should().ContainSingle().Subject;
        rule.Key.Should().Be("acme ferragens");
        rule.CategoryId.Should().Be(category.Id);
        rule.CategoryName.Should().Be("Ferragens e obras");
        rule.HitCount.Should().Be(1);

        // 2ª importação: outra data, valor e código na descrição, mesmo estabelecimento
        var second = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55501", "A2", new DateTime(2026, 2, 20), -75m, "COMPRA CARTAO 20/02 ACME FERRAGENS 9876"));

        var secondRow = second.Transactions.Should().ContainSingle().Subject;
        secondRow.SuggestedCategoryId.Should().Be(category.Id);
        secondRow.SuggestedCategoryName.Should().Be("Ferragens e obras");
        secondRow.SuggestionSource.Should().Be(CategorySuggestionSource.Rule);

        await ConfirmAsync(s, second, category.Id);
        (await ListRulesAsync(s)).Single().HitCount.Should().Be(2);
    }

    [Fact]
    public async Task Confirmar_DeveTrocarACategoriaDaRegra_QuandoOUsuarioMuda()
    {
        var s = await AuthenticateAsync("regras.troca@teste.com");
        var account = await CreateAccountAsync(s, "Conta Troca");
        var antiga = await CreateCategoryAsync(s, "Obras");
        var nova = await CreateCategoryAsync(s, "Manutenção");

        var first = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55502", "B1", new DateTime(2026, 2, 5), -50m, "ACME PARAFUSOS 111"));
        await ConfirmAsync(s, first, antiga.Id);

        var second = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55502", "B2", new DateTime(2026, 2, 15), -60m, "ACME PARAFUSOS 222"));
        second.Transactions.Single().SuggestedCategoryId.Should().Be(antiga.Id);
        await ConfirmAsync(s, second, nova.Id);

        var rule = (await ListRulesAsync(s)).Should().ContainSingle().Subject;
        rule.CategoryId.Should().Be(nova.Id);
        rule.HitCount.Should().Be(1);
    }

    // DELETE /api/categoryrules/{id}

    [Fact]
    public async Task Delete_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().DeleteAsync($"/api/categoryrules/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoARegraNaoExiste()
    {
        var s = await AuthenticateAsync("regras.404@teste.com");

        var response = await s.Client.DeleteAsync($"/api/categoryrules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveEsquecerARegra_EAProximaImportacaoNaoSugerir()
    {
        var s = await AuthenticateAsync("regras.esquece@teste.com");
        var account = await CreateAccountAsync(s, "Conta Esquece");
        var category = await CreateCategoryAsync(s, "Serviços gerais");

        var first = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55503", "C1", new DateTime(2026, 2, 3), -90m, "ACME SERVICOS 321"));
        await ConfirmAsync(s, first, category.Id);
        var rule = (await ListRulesAsync(s)).Single();

        var delete = await s.Client.DeleteAsync($"/api/categoryrules/{rule.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListRulesAsync(s)).Should().BeEmpty();

        var second = await UploadAndProcessAsync(s, account.Id,
            BuildOfx("55503", "C2", new DateTime(2026, 2, 18), -95m, "ACME SERVICOS 654"));
        second.Transactions.Single().SuggestedCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Regras_DevemSerIsoladasPorUsuario()
    {
        var owner = await AuthenticateAsync("regras.dono@teste.com");
        var other = await AuthenticateAsync("regras.outro@teste.com");
        var account = await CreateAccountAsync(owner, "Conta Dono");
        var category = await CreateCategoryAsync(owner, "Só do dono");

        var preview = await UploadAndProcessAsync(owner, account.Id,
            BuildOfx("55504", "D1", new DateTime(2026, 2, 7), -30m, "ACME PRIVADO 777"));
        await ConfirmAsync(owner, preview, category.Id);
        var rule = (await ListRulesAsync(owner)).Single();

        (await ListRulesAsync(other)).Should().BeEmpty();

        var response = await other.Client.DeleteAsync($"/api/categoryrules/{rule.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await ListRulesAsync(owner)).Should().ContainSingle();
    }
}
