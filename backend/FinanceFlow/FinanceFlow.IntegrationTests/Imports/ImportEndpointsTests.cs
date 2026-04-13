using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Imports;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Imports;

public class ImportEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Import Teste",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(email, "Teste@123"));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    private static MultipartFormDataContent CreateOFXContent(string ofxContent = "") =>
        new()
        {
            {
                new StreamContent(
                    new MemoryStream(Encoding.UTF8.GetBytes(
                        string.IsNullOrEmpty(ofxContent) ? GetSampleOFX() : ofxContent)))
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                },
                "file",
                "extrato.ofx"
            }
        };

    private static string GetSampleOFX() => """
        OFXHEADER:100
        DATA:OFXSGML
        VERSION:102
        SECURITY:NONE
        ENCODING:USASCII
        CHARSET:1252
        COMPRESSION:NONE
        OLDFILEUID:NONE
        NEWFILEUID:NONE
        <OFX>
        <SIGNONMSGSRSV1>
        <SONRS>
        <STATUS><CODE>0</CODE><SEVERITY>INFO</SEVERITY></STATUS>
        <DTSERVER>20260101</DTSERVER>
        <LANGUAGE>POR</LANGUAGE>
        </SONRS>
        </SIGNONMSGSRSV1>
        <BANKMSGSRSV1>
        <STMTTRNRS>
        <TRNUID>1</TRNUID>
        <STMTRS>
        <CURDEF>BRL</CURDEF>
        <BANKACCTFROM>
        <BANKID>001</BANKID>
        <ACCTID>12345</ACCTID>
        <ACCTTYPE>CHECKING</ACCTTYPE>
        </BANKACCTFROM>
        <BANKTRANLIST>
        <DTSTART>20260101</DTSTART>
        <DTEND>20260131</DTEND>
        <STMTTRN>
        <TRNTYPE>DEBIT</TRNTYPE>
        <DTPOSTED>20260115</DTPOSTED>
        <TRNAMT>-150.00</TRNAMT>
        <FITID>FIT001</FITID>
        <MEMO>IFOOD RESTAURANTE</MEMO>
        </STMTTRN>
        </BANKTRANLIST>
        <LEDGERBAL>
        <BALAMT>1000.00</BALAMT>
        <DTASOF>20260131</DTASOF>
        </LEDGERBAL>
        </STMTRS>
        </STMTTRNRS>
        </BANKMSGSRSV1>
        </OFX>
        """;

    // POST /api/imports/ofx

    [Fact]
    public async Task UploadOFX_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.PostAsync("/api/imports/ofx", CreateOFXContent());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadOFX_DeveRetornar400OuUnprocessable_QuandoNenhumFicheiroEnviado()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "import.nofile@teste.com");

        var response = await client.PostAsync("/api/imports/ofx",
            new MultipartFormDataContent());

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadOFX_DeveRetornar422_QuandoFicheiroNaoEhOFX()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "import.notofx@teste.com");

        var content = new MultipartFormDataContent
        {
            {
                new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("csv,data"))),
                "file",
                "extrato.csv"
            }
        };

        var response = await client.PostAsync("/api/imports/ofx", content);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // GET /api/imports

    [Fact]
    public async Task GetAll_DeveRetornar401_QuandoSemToken()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/imports");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_DeveRetornarListaVazia_QuandoNaoExistemImportacoes()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "import.getall.empty@teste.com");

        var response = await client.GetAsync("/api/imports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<BankImportDto>>();
        result.Should().BeEmpty();
    }

    // GET /api/imports/{id}/preview

    [Fact]
    public async Task GetPreview_DeveRetornar404_QuandoImportacaoNaoExiste()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "import.preview.notfound@teste.com");

        var response = await client.GetAsync($"/api/imports/{Guid.NewGuid()}/preview");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST /api/imports/{id}/confirm

    [Fact]
    public async Task Confirm_DeveRetornar404_QuandoImportacaoNaoExiste()
    {
        var client = CreateClient();
        await AuthenticateAsync(client, "import.confirm.notfound@teste.com");

        var response = await client.PostAsJsonAsync(
            $"/api/imports/{Guid.NewGuid()}/confirm",
            new ConfirmImportRequestDto([]));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
