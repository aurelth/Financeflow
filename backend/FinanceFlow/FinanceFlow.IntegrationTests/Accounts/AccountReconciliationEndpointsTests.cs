using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Accounts;

public class AccountReconciliationEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private Guid _userId;

    // Setup

    private async Task AuthenticateAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Conciliação Teste",
            Email: email,
            Password: "Teste@123",
            Cpf: TestCpfGenerator.Next(),
            Gender: "Male",
            Currency: "BRL",
            Timezone: "America/Sao_Paulo"));

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, "Teste@123"));
        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadAsJsonAsync<AuthResponseDto>();
        _userId = auth!.User.Id;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private async Task<AccountDto> CreateAccountAsync(string name, decimal initialBalance)
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequestDto(
            Name: name,
            Type: AccountType.Checking,
            BankName: null,
            InitialBalance: initialBalance,
            InitialBalanceDate: new DateTime(2020, 1, 1),
            Currency: null,
            Color: null,
            Icon: null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<AccountDto>())!;
    }

    private async Task<Guid> CreateCategoryAsync(string name, TransactionType type)
    {
        var response = await _client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequestDto(name, "🏷️", "#6366f1", type));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<CategoryDto>())!.Id;
    }

    private record OfxRow(DateTime Date, decimal Amount, string FitId, string Memo);

    private static string BuildOfx(string acctId, string? ledgerBalance, params OfxRow[] rows)
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

        foreach (var row in rows)
        {
            sb.AppendLine("<STMTTRN>");
            sb.AppendLine($"<TRNTYPE>{(row.Amount < 0 ? "DEBIT" : "CREDIT")}</TRNTYPE>");
            sb.AppendLine($"<DTPOSTED>{row.Date:yyyyMMdd}</DTPOSTED>");
            sb.AppendLine($"<TRNAMT>{row.Amount.ToString("F2", CultureInfo.InvariantCulture)}</TRNAMT>");
            sb.AppendLine($"<FITID>{row.FitId}</FITID>");
            sb.AppendLine($"<MEMO>{row.Memo}</MEMO>");
            sb.AppendLine("</STMTTRN>");
        }

        sb.AppendLine("</BANKTRANLIST>");
        if (ledgerBalance is not null)
            sb.AppendLine($"<LEDGERBAL><BALAMT>{ledgerBalance}</BALAMT><DTASOF>20260228</DTASOF></LEDGERBAL>");
        sb.AppendLine("</STMTRS></STMTTRNRS></BANKMSGSRSV1></OFX>");
        return sb.ToString();
    }

    private async Task<BankImportPreviewDto> UploadAndProcessAsync(Guid accountId, string ofx)
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

        var upload = await _client.PostAsync("/api/imports/ofx", content);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        var import = (await upload.Content.ReadAsJsonAsync<BankImportDto>())!;

        var process = await _client.PostAsJsonAsync(
            $"/api/imports/{import.Id}/process", new ProcessImportRequestDto(_userId));
        process.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var preview = await _client.GetAsync($"/api/imports/{import.Id}/preview");
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await preview.Content.ReadAsJsonAsync<BankImportPreviewDto>())!;
    }

    private async Task ConfirmAllAsync(BankImportPreviewDto preview, Guid incomeCategory, Guid expenseCategory)
    {
        var items = preview.Transactions.Select(t => new ConfirmImportItemDto(
            t.Id,
            true,
            t.Type == TransactionType.Income ? incomeCategory : expenseCategory,
            t.Type.ToString())).ToArray();

        var response = await _client.PostAsJsonAsync(
            $"/api/imports/{preview.ImportId}/confirm", new ConfirmImportRequestDto(items));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<AccountReconciliationDto> GetReconciliationAsync(Guid accountId)
    {
        var response = await _client.GetAsync($"/api/accounts/{accountId}/reconciliation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadAsJsonAsync<AccountReconciliationDto>())!;
    }

    private async Task CreateTransactionAsync(
        Guid accountId, decimal amount, TransactionType type, DateTime date, Guid categoryId)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(amount.ToString("F2", CultureInfo.InvariantCulture)), "amount" },
            { new StringContent(((int)type).ToString()), "type" },
            { new StringContent(date.ToString("o")), "date" },
            { new StringContent("Lançamento manual"), "description" },
            { new StringContent(((int)TransactionStatus.Paid).ToString()), "status" },
            { new StringContent("false"), "isRecurring" },
            { new StringContent(((int)RecurrenceType.None).ToString()), "recurrenceType" },
            { new StringContent(categoryId.ToString()), "categoryId" },
            { new StringContent(accountId.ToString()), "accountId" },
        };

        var response = await _client.PostAsync("/api/transactions", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    // Autorização e posse

    [Fact]
    public async Task Reconciliation_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().GetAsync($"/api/accounts/{Guid.NewGuid()}/reconciliation");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reconciliation_DeveRetornar404_QuandoContaNaoExiste()
    {
        await AuthenticateAsync("conciliacao.404@teste.com");

        var response = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}/reconciliation");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Fluxo principal

    [Fact]
    public async Task Reconciliation_DeveIndicarSemExtrato_EnquantoAImportacaoNaoForConfirmada()
    {
        await AuthenticateAsync("conciliacao.semconfirmar@teste.com");
        var account = await CreateAccountAsync("Conta A", 1000m);

        var preview = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "1400.00",
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO"),
            new OfxRow(new DateTime(2026, 2, 12), -100m, "A2", "MERCADO")));

        preview.Transactions.Should().HaveCount(2);

        // Antes de confirmar os lançamentos ainda não existem na conta: comparar daria diferença falsa
        var reconciliation = await GetReconciliationAsync(account.Id);
        reconciliation.Status.Should().Be(ReconciliationStatus.NoStatement);
    }

    [Fact]
    public async Task Reconciliation_DeveConciliar_QuandoSaldoDoExtratoBateComOCalculado()
    {
        await AuthenticateAsync("conciliacao.ok@teste.com");
        var account = await CreateAccountAsync("Conta A", 1000m);
        var income = await CreateCategoryAsync("Conc receita ok", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa ok", TransactionType.Expense);

        var preview = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "1400.00",
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO"),
            new OfxRow(new DateTime(2026, 2, 12), -100m, "A2", "MERCADO")));
        await ConfirmAllAsync(preview, income, expense);

        var reconciliation = await GetReconciliationAsync(account.Id);

        reconciliation.Status.Should().Be(ReconciliationStatus.Matched);
        reconciliation.StatementBalance.Should().Be(1400m);
        reconciliation.CalculatedBalance.Should().Be(1400m);
        reconciliation.Difference.Should().Be(0m);
        reconciliation.StatementDate!.Value.Date.Should().Be(new DateTime(2026, 2, 28));
        reconciliation.ImportId.Should().Be(preview.ImportId);
    }

    [Fact]
    public async Task Reconciliation_DeveApontarDiferenca_QuandoSaldoDoExtratoNaoBate()
    {
        await AuthenticateAsync("conciliacao.diferenca@teste.com");
        var account = await CreateAccountAsync("Conta A", 1000m);
        var income = await CreateCategoryAsync("Conc receita dif", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa dif", TransactionType.Expense);

        // O banco diz 1500, mas os lançamentos do extrato só levam a conta a 1400
        var preview = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "1500.00",
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO"),
            new OfxRow(new DateTime(2026, 2, 12), -100m, "A2", "MERCADO")));
        await ConfirmAllAsync(preview, income, expense);

        var reconciliation = await GetReconciliationAsync(account.Id);

        reconciliation.Status.Should().Be(ReconciliationStatus.Divergent);
        reconciliation.CalculatedBalance.Should().Be(1400m);
        reconciliation.Difference.Should().Be(100m);
    }

    [Fact]
    public async Task Reconciliation_DeveRefletirLancamentoManualAnteriorAoExtrato_MasNaoOPosterior()
    {
        await AuthenticateAsync("conciliacao.manual@teste.com");
        var account = await CreateAccountAsync("Conta A", 1000m);
        var income = await CreateCategoryAsync("Conc receita man", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa man", TransactionType.Expense);

        var preview = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "1400.00",
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO"),
            new OfxRow(new DateTime(2026, 2, 12), -100m, "A2", "MERCADO")));
        await ConfirmAllAsync(preview, income, expense);

        // Depois da data do extrato: não muda a conciliação daquele extrato
        await CreateTransactionAsync(account.Id, 40m, TransactionType.Expense, new DateTime(2026, 3, 5), expense);
        (await GetReconciliationAsync(account.Id)).Status.Should().Be(ReconciliationStatus.Matched);

        // No mesmo dia do extrato (inclusive): entra no cálculo e gera diferença
        await CreateTransactionAsync(account.Id, 25m, TransactionType.Expense, new DateTime(2026, 2, 28), expense);
        var reconciliation = await GetReconciliationAsync(account.Id);

        reconciliation.Status.Should().Be(ReconciliationStatus.Divergent);
        reconciliation.CalculatedBalance.Should().Be(1375m);
        reconciliation.Difference.Should().Be(25m);
    }

    [Fact]
    public async Task Reconciliation_DeveImportarOfxSemLedgerBalance_ENaoConciliar()
    {
        await AuthenticateAsync("conciliacao.semsaldo@teste.com");
        var account = await CreateAccountAsync("Conta A", 1000m);
        var income = await CreateCategoryAsync("Conc receita ss", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa ss", TransactionType.Expense);

        // LEDGERBAL é opcional no OFX: o arquivo precisa ser aceito mesmo sem ele
        var preview = await UploadAndProcessAsync(account.Id, BuildOfx("11111", ledgerBalance: null,
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO")));
        preview.Transactions.Should().HaveCount(1);
        await ConfirmAllAsync(preview, income, expense);

        var reconciliation = await GetReconciliationAsync(account.Id);

        reconciliation.Status.Should().Be(ReconciliationStatus.NoStatement);
    }

    [Fact]
    public async Task Reconciliation_DeveIgnorarExtratoAindaNaoConfirmado()
    {
        await AuthenticateAsync("conciliacao.naoconfirmado@teste.com");
        var account = await CreateAccountAsync("Conta A", 0m);
        var income = await CreateCategoryAsync("Conc receita mr", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa mr", TransactionType.Expense);

        var first = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "300.00",
            new OfxRow(new DateTime(2026, 2, 10), 300m, "B1", "SALARIO")));
        await ConfirmAllAsync(first, income, expense);

        var second = await UploadAndProcessAsync(account.Id, BuildOfx("11111", "300.00",
            new OfxRow(new DateTime(2026, 2, 10), 300m, "B1", "SALARIO")));

        // O segundo arquivo só tem duplicatas e não foi confirmado: continua valendo o primeiro
        var reconciliation = await GetReconciliationAsync(account.Id);

        reconciliation.Status.Should().Be(ReconciliationStatus.Matched);
        reconciliation.ImportId.Should().Be(first.ImportId);
        second.ImportId.Should().NotBe(first.ImportId);
    }

    // Lista

    [Fact]
    public async Task ReconciliationList_DeveRetornarUmaEntradaPorContaAtiva()
    {
        await AuthenticateAsync("conciliacao.lista@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var income = await CreateCategoryAsync("Conc receita ls", TransactionType.Income);
        var expense = await CreateCategoryAsync("Conc despesa ls", TransactionType.Expense);

        var preview = await UploadAndProcessAsync(a.Id, BuildOfx("11111", "1500.00",
            new OfxRow(new DateTime(2026, 2, 10), 500m, "A1", "SALARIO")));
        await ConfirmAllAsync(preview, income, expense);

        var response = await _client.GetAsync("/api/accounts/reconciliation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await response.Content.ReadAsJsonAsync<List<AccountReconciliationDto>>())!;

        list.Single(r => r.AccountId == a.Id).Status.Should().Be(ReconciliationStatus.Matched);
        list.Single(r => r.AccountId == b.Id).Status.Should().Be(ReconciliationStatus.NoStatement);
    }
}
