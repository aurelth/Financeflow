using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.DTOs.Transfers;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Transfers;

public class TransferEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private Guid _userId;

    // Setup

    private async Task AuthenticateAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Transfer Teste",
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

    private async Task<AccountDto> CreateAccountAsync(string name, decimal initialBalance = 0m)
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

    private async Task<decimal> BalanceOfAsync(Guid accountId)
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountDto>>("/api/accounts?includeArchived=true",
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        return accounts!.Single(a => a.Id == accountId).Balance;
    }

    private async Task<TransactionDto> CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        TransactionType type,
        DateTime date,
        string description,
        Guid categoryId,
        TransferDirection? direction = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(amount.ToString("F2", CultureInfo.InvariantCulture)), "amount" },
            { new StringContent(((int)type).ToString()), "type" },
            { new StringContent(date.ToString("o")), "date" },
            { new StringContent(description), "description" },
            { new StringContent(((int)TransactionStatus.Paid).ToString()), "status" },
            { new StringContent("false"), "isRecurring" },
            { new StringContent(((int)RecurrenceType.None).ToString()), "recurrenceType" },
            { new StringContent(categoryId.ToString()), "categoryId" },
            { new StringContent(accountId.ToString()), "accountId" },
        };

        if (direction.HasValue)
            form.Add(new StringContent(direction.Value.ToString()), "transferDirection");

        var response = await _client.PostAsync("/api/transactions", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadAsJsonAsync<TransactionDto>())!;
    }

    private async Task<TransactionDto> GetTransactionAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/transactions/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadAsJsonAsync<TransactionDto>())!;
    }

    // OFX / importação

    private record OfxRow(DateTime Date, decimal Amount, string FitId, string Memo);

    private static string BuildOfx(string acctId, params OfxRow[] rows)
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
        sb.AppendLine("<LEDGERBAL><BALAMT>0.00</BALAMT><DTASOF>20260228</DTASOF></LEDGERBAL>");
        sb.AppendLine("</STMTRS></STMTTRNRS></BANKMSGSRSV1></OFX>");
        return sb.ToString();
    }

    /// <summary>Envia o OFX para a conta e roda o processamento que, em produção, é feito pelo Worker.</summary>
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

        return await GetPreviewAsync(import.Id);
    }

    private async Task<BankImportPreviewDto> GetPreviewAsync(Guid importId)
    {
        var response = await _client.GetAsync($"/api/imports/{importId}/preview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadAsJsonAsync<BankImportPreviewDto>())!;
    }

    private async Task ConfirmAsync(Guid importId, params ConfirmImportItemDto[] items)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/imports/{importId}/confirm", new ConfirmImportRequestDto(items));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static BankImportTransactionDto Row(BankImportPreviewDto preview, string memoPart) =>
        preview.Transactions.Single(t => t.Description.Contains(memoPart, StringComparison.OrdinalIgnoreCase));

    private async Task<List<TransactionDto>> ListTransactionsAsync()
    {
        var response = await _client.GetAsync("/api/transactions?pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadAsJsonAsync<PagedResultDto<TransactionDto>>();
        return page!.Items.ToList();
    }

    // POST /api/transfers

    [Fact]
    public async Task Create_DeveRetornar401_QuandoSemToken()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(Guid.NewGuid(), Guid.NewGuid(), 10m, DateTime.UtcNow));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_DeveCriarAsDuasPontas_EAjustarOsSaldosDasContas()
    {
        await AuthenticateAsync("transfer.create@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);

        var response = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 250m, new DateTime(2026, 2, 10), "Reserva"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var link = (await response.Content.ReadAsJsonAsync<TransferLinkDto>())!;

        link.Out.AccountId.Should().Be(a.Id);
        link.Out.Direction.Should().Be(TransferDirection.Out);
        link.In.AccountId.Should().Be(b.Id);
        link.In.Direction.Should().Be(TransferDirection.In);
        link.Origin.Should().Be("Manual");

        (await BalanceOfAsync(a.Id)).Should().Be(750m);
        (await BalanceOfAsync(b.Id)).Should().Be(250m);

        var outTx = await GetTransactionAsync(link.Out.TransactionId);
        outTx.Type.Should().Be(TransactionType.Transfer);
        outTx.TransferLinkId.Should().Be(link.Id);
        outTx.CounterpartTransactionId.Should().Be(link.In.TransactionId);
        outTx.CounterpartAccountName.Should().Be("Conta B");
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoContasIguais()
    {
        await AuthenticateAsync("transfer.same@teste.com");
        var a = await CreateAccountAsync("Conta A");

        var response = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, a.Id, 10m, DateTime.UtcNow));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar404_QuandoContaNaoExiste()
    {
        await AuthenticateAsync("transfer.notfound@teste.com");
        var a = await CreateAccountAsync("Conta A");

        var response = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, Guid.NewGuid(), 10m, DateTime.UtcNow));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST /api/transfers/link  e  DELETE /api/transfers/{id}

    [Fact]
    public async Task Link_DeveConverterReceitaEDespesaEmTransferencia_SemMudarOsSaldos()
    {
        await AuthenticateAsync("transfer.link@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var expenseCategory = await CreateCategoryAsync("Saques", TransactionType.Expense);
        var incomeCategory = await CreateCategoryAsync("Depósitos", TransactionType.Income);

        var date = new DateTime(2026, 2, 10);
        var expense = await CreateTransactionAsync(a.Id, 300m, TransactionType.Expense, date, "Saída", expenseCategory);
        var income = await CreateTransactionAsync(b.Id, 300m, TransactionType.Income, date, "Entrada", incomeCategory);

        var response = await _client.PostAsJsonAsync("/api/transfers/link",
            new LinkTransfersRequestDto(income.Id, expense.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = (await response.Content.ReadAsJsonAsync<TransferLinkDto>())!;
        link.Out.TransactionId.Should().Be(expense.Id);
        link.In.TransactionId.Should().Be(income.Id);

        (await GetTransactionAsync(expense.Id)).Type.Should().Be(TransactionType.Transfer);
        (await GetTransactionAsync(income.Id)).Type.Should().Be(TransactionType.Transfer);
        (await BalanceOfAsync(a.Id)).Should().Be(700m);
        (await BalanceOfAsync(b.Id)).Should().Be(300m);
    }

    [Fact]
    public async Task Link_DeveRetornar422_QuandoValoresDiferentes()
    {
        await AuthenticateAsync("transfer.linkdiff@teste.com");
        var a = await CreateAccountAsync("Conta A");
        var b = await CreateAccountAsync("Conta B");
        var expenseCategory = await CreateCategoryAsync("Saques", TransactionType.Expense);
        var incomeCategory = await CreateCategoryAsync("Depósitos", TransactionType.Income);

        var date = new DateTime(2026, 2, 10);
        var expense = await CreateTransactionAsync(a.Id, 300m, TransactionType.Expense, date, "Saída", expenseCategory);
        var income = await CreateTransactionAsync(b.Id, 299m, TransactionType.Income, date, "Entrada", incomeCategory);

        var response = await _client.PostAsJsonAsync("/api/transfers/link",
            new LinkTransfersRequestDto(expense.Id, income.Id));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Unlink_DeveDevolverAsPontasComoDespesaEReceita()
    {
        await AuthenticateAsync("transfer.unlink@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var expenseCategory = await CreateCategoryAsync("Saques", TransactionType.Expense);
        var incomeCategory = await CreateCategoryAsync("Depósitos", TransactionType.Income);

        var created = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 100m, new DateTime(2026, 2, 10)));
        var link = (await created.Content.ReadAsJsonAsync<TransferLinkDto>())!;

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/transfers/{link.Id}")
        {
            Content = JsonContent.Create(new UnlinkTransferRequestDto(expenseCategory, incomeCategory, false))
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var outTx = await GetTransactionAsync(link.Out.TransactionId);
        var inTx = await GetTransactionAsync(link.In.TransactionId);
        outTx.Type.Should().Be(TransactionType.Expense);
        outTx.CategoryId.Should().Be(expenseCategory);
        outTx.TransferLinkId.Should().BeNull();
        inTx.Type.Should().Be(TransactionType.Income);
        inTx.CategoryId.Should().Be(incomeCategory);

        // O saldo de cada conta continua igual: só mudou a classificação
        (await BalanceOfAsync(a.Id)).Should().Be(900m);
        (await BalanceOfAsync(b.Id)).Should().Be(100m);
    }

    [Fact]
    public async Task Unlink_DeveExcluirAsDuasPontas_QuandoPedido()
    {
        await AuthenticateAsync("transfer.unlinkdelete@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);

        var created = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 100m, new DateTime(2026, 2, 10)));
        var link = (await created.Content.ReadAsJsonAsync<TransferLinkDto>())!;

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/transfers/{link.Id}")
        {
            Content = JsonContent.Create(new UnlinkTransferRequestDto(null, null, true))
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/transactions/{link.Out.TransactionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/transactions/{link.In.TransactionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await BalanceOfAsync(a.Id)).Should().Be(1000m);
        (await BalanceOfAsync(b.Id)).Should().Be(0m);
    }

    [Fact]
    public async Task Unlink_DeveRetornar422_QuandoFaltaCategoria()
    {
        await AuthenticateAsync("transfer.unlinkmissing@teste.com");
        var a = await CreateAccountAsync("Conta A");
        var b = await CreateAccountAsync("Conta B");

        var created = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 100m, new DateTime(2026, 2, 10)));
        var link = (await created.Content.ReadAsJsonAsync<TransferLinkDto>())!;

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/transfers/{link.Id}")
        {
            Content = JsonContent.Create(new UnlinkTransferRequestDto(null, null, false))
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // Lançamentos de transferência

    [Fact]
    public async Task CreateTransaction_DeveRetornar422_QuandoTransferenciaSemSentido()
    {
        await AuthenticateAsync("transfer.nodirection@teste.com");
        var a = await CreateAccountAsync("Conta A");

        var form = new MultipartFormDataContent
        {
            { new StringContent("10.00"), "amount" },
            { new StringContent(((int)TransactionType.Transfer).ToString()), "type" },
            { new StringContent(DateTime.UtcNow.ToString("o")), "date" },
            { new StringContent("Sem sentido"), "description" },
            { new StringContent(((int)TransactionStatus.Paid).ToString()), "status" },
            { new StringContent("false"), "isRecurring" },
            { new StringContent("0"), "recurrenceType" },
            { new StringContent(WellKnownIds.TransferCategoryId.ToString()), "categoryId" },
            { new StringContent(a.Id.ToString()), "accountId" },
        };

        var response = await _client.PostAsync("/api/transactions", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateTransaction_DeveRetornar422_QuandoAlteraValorDeTransferenciaVinculada()
    {
        await AuthenticateAsync("transfer.updatelinked@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B");

        var created = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 100m, new DateTime(2026, 2, 10)));
        var link = (await created.Content.ReadAsJsonAsync<TransferLinkDto>())!;
        var tx = await GetTransactionAsync(link.Out.TransactionId);

        var response = await _client.PutAsJsonAsync($"/api/transactions/{tx.Id}", new UpdateTransactionRequestDto(
            Amount: 150m,
            Type: tx.Type,
            Date: tx.Date,
            Description: tx.Description,
            Status: tx.Status,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: tx.CategoryId,
            SubcategoryId: null,
            Tags: [],
            TransferDirection: tx.TransferDirection));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteTransaction_DeveDesfazerOVinculo_EDeixarAOutraPontaPendente()
    {
        await AuthenticateAsync("transfer.deleteside@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B");

        var created = await _client.PostAsJsonAsync("/api/transfers",
            new CreateTransferRequestDto(a.Id, b.Id, 100m, new DateTime(2026, 2, 10)));
        var link = (await created.Content.ReadAsJsonAsync<TransferLinkDto>())!;

        var delete = await _client.DeleteAsync($"/api/transactions/{link.Out.TransactionId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await GetTransactionAsync(link.In.TransactionId);
        remaining.TransferLinkId.Should().BeNull();
        remaining.TransferDirection.Should().Be(TransferDirection.In);

        var pending = await _client.GetFromJsonAsync<List<PendingTransferDto>>("/api/transfers/pending",
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        pending.Should().ContainSingle(p => p.Transaction.TransactionId == link.In.TransactionId);
    }

    // GET /api/transfers/pending

    [Fact]
    public async Task Pending_DeveSugerirOParDeOutraConta()
    {
        await AuthenticateAsync("transfer.pending@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B");
        var incomeCategory = await CreateCategoryAsync("Depósitos", TransactionType.Income);

        var date = new DateTime(2026, 2, 10);
        var outTx = await CreateTransactionAsync(a.Id, 400m, TransactionType.Transfer, date,
            "PIX ENVIADO CONTA B", WellKnownIds.TransferCategoryId, TransferDirection.Out);
        var income = await CreateTransactionAsync(b.Id, 400m, TransactionType.Income, date,
            "PIX RECEBIDO CONTA A", incomeCategory);

        var response = await _client.GetAsync("/api/transfers/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pending = (await response.Content.ReadAsJsonAsync<List<PendingTransferDto>>())!;

        var item = pending.Single(p => p.Transaction.TransactionId == outTx.Id);
        item.Suggestion.Should().NotBeNull();
        item.Suggestion!.Candidate.TransactionId.Should().Be(income.Id);
        item.Suggestion.WillConvert.Should().BeTrue();
        item.Suggestion.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    // Aceite: salário + Pix entre contas + gasto, importando A → B

    [Fact]
    public async Task Importacao_ContaAdepoisContaB_DevePareaPixAutomaticamente()
    {
        await AuthenticateAsync("transfer.import.ab@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var incomeCategory = await CreateCategoryAsync("Salário do teste", TransactionType.Income);
        var expenseCategory = await CreateCategoryAsync("Mercado do teste", TransactionType.Expense);

        // 1) Extrato da conta A: salário, Pix enviado e um gasto
        var previewA = await UploadAndProcessAsync(a.Id, BuildOfx("11111",
            new OfxRow(new DateTime(2026, 2, 10), 5000m, "A1", "SALARIO EMPRESA XYZ"),
            new OfxRow(new DateTime(2026, 2, 12), -1000m, "A2", "PIX ENVIADO CONTA B"),
            new OfxRow(new DateTime(2026, 2, 13), -50m, "A3", "MERCADO CENTRAL")));

        previewA.Transactions.Should().OnlyContain(t => t.TransferMatch == null,
            "ainda não há lançamentos em outras contas");

        // O Pix ainda não tem par: o usuário o marca como transferência (fica pendente)
        await ConfirmAsync(previewA.ImportId,
            new ConfirmImportItemDto(Row(previewA, "SALARIO").Id, true, incomeCategory, "Income"),
            new ConfirmImportItemDto(Row(previewA, "PIX").Id, true, Guid.Empty, "Transfer"),
            new ConfirmImportItemDto(Row(previewA, "MERCADO").Id, true, expenseCategory, "Expense"));

        var pendingBefore = (await (await _client.GetAsync("/api/transfers/pending"))
            .Content.ReadAsJsonAsync<List<PendingTransferDto>>())!;
        pendingBefore.Should().ContainSingle(p => p.Transaction.Direction == TransferDirection.Out);

        // 2) Extrato da conta B: o Pix recebido encontra a saída da conta A
        var previewB = await UploadAndProcessAsync(b.Id, BuildOfx("22222",
            new OfxRow(new DateTime(2026, 2, 12), 1000m, "B1", "PIX RECEBIDO CONTA A")));

        var pixIn = Row(previewB, "PIX");
        pixIn.TransferMatch.Should().NotBeNull();
        pixIn.TransferMatch!.Status.Should().Be("Auto");
        pixIn.TransferMatch.AccountName.Should().Be("Conta A");
        pixIn.TransferMatch.WillConvert.Should().BeFalse();
        previewB.AutoTransfers.Should().Be(1);

        await ConfirmAsync(previewB.ImportId,
            new ConfirmImportItemDto(pixIn.Id, true, Guid.Empty, "Transfer", TransferAction: "Link"));

        // 3) Resultado: Pix pareado, sem pendências, saldos corretos e sem dupla contagem
        var transactions = await ListTransactionsAsync();
        var pixOut = transactions.Single(t => t.AccountId == a.Id && t.Type == TransactionType.Transfer);
        var pixInTx = transactions.Single(t => t.AccountId == b.Id);

        pixOut.TransferLinkId.Should().NotBeNull();
        pixOut.TransferLinkId.Should().Be(pixInTx.TransferLinkId);
        pixOut.CounterpartTransactionId.Should().Be(pixInTx.Id);
        pixInTx.Type.Should().Be(TransactionType.Transfer);
        pixInTx.TransferDirection.Should().Be(TransferDirection.In);

        (await BalanceOfAsync(a.Id)).Should().Be(1000m + 5000m - 1000m - 50m);
        (await BalanceOfAsync(b.Id)).Should().Be(1000m);

        var pendingAfter = (await (await _client.GetAsync("/api/transfers/pending"))
            .Content.ReadAsJsonAsync<List<PendingTransferDto>>())!;
        pendingAfter.Should().BeEmpty();
    }

    // Aceite: o mesmo cenário na ordem inversa (B → A)

    [Fact]
    public async Task Importacao_ContaBdepoisContaA_DeveConverterAReceitaEmTransferencia()
    {
        await AuthenticateAsync("transfer.import.ba@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var incomeCategory = await CreateCategoryAsync("Salário do teste", TransactionType.Income);
        var expenseCategory = await CreateCategoryAsync("Mercado do teste", TransactionType.Expense);

        // 1) Extrato da conta B primeiro: o Pix recebido entra como receita comum
        var previewB = await UploadAndProcessAsync(b.Id, BuildOfx("22222",
            new OfxRow(new DateTime(2026, 2, 12), 1000m, "B1", "PIX RECEBIDO CONTA A")));

        await ConfirmAsync(previewB.ImportId,
            new ConfirmImportItemDto(Row(previewB, "PIX").Id, true, incomeCategory, "Income"));

        // 2) Extrato da conta A: o Pix enviado encontra a receita da conta B
        var previewA = await UploadAndProcessAsync(a.Id, BuildOfx("11111",
            new OfxRow(new DateTime(2026, 2, 10), 5000m, "A1", "SALARIO EMPRESA XYZ"),
            new OfxRow(new DateTime(2026, 2, 12), -1000m, "A2", "PIX ENVIADO CONTA B"),
            new OfxRow(new DateTime(2026, 2, 13), -50m, "A3", "MERCADO CENTRAL")));

        var pixOut = Row(previewA, "PIX");
        pixOut.TransferMatch.Should().NotBeNull();
        pixOut.TransferMatch!.Status.Should().Be("Auto");
        pixOut.TransferMatch.AccountName.Should().Be("Conta B");
        pixOut.TransferMatch.WillConvert.Should().BeTrue();

        // Salário e mercado não têm par: o gasto e a receita de outra origem seguem como estão
        Row(previewA, "SALARIO").TransferMatch.Should().BeNull();
        Row(previewA, "MERCADO").TransferMatch.Should().BeNull();

        await ConfirmAsync(previewA.ImportId,
            new ConfirmImportItemDto(Row(previewA, "SALARIO").Id, true, incomeCategory, "Income"),
            new ConfirmImportItemDto(pixOut.Id, true, Guid.Empty, "Transfer", TransferAction: "Link"),
            new ConfirmImportItemDto(Row(previewA, "MERCADO").Id, true, expenseCategory, "Expense"));

        // 3) A receita da conta B virou a entrada da transferência
        var transactions = await ListTransactionsAsync();
        var pixInTx = transactions.Single(t => t.AccountId == b.Id);
        var pixOutTx = transactions.Single(t => t.AccountId == a.Id && t.Type == TransactionType.Transfer);

        pixInTx.Type.Should().Be(TransactionType.Transfer);
        pixInTx.TransferDirection.Should().Be(TransferDirection.In);
        pixInTx.CategoryId.Should().Be(WellKnownIds.TransferCategoryId);
        pixInTx.CounterpartTransactionId.Should().Be(pixOutTx.Id);

        (await BalanceOfAsync(a.Id)).Should().Be(1000m + 5000m - 1000m - 50m);
        (await BalanceOfAsync(b.Id)).Should().Be(1000m);
    }

    [Fact]
    public async Task Importacao_SugestaoIgnorada_DeveImportarComoLancamentoComum_ENaoVincular()
    {
        await AuthenticateAsync("transfer.import.ignore@teste.com");
        var a = await CreateAccountAsync("Conta A", 1000m);
        var b = await CreateAccountAsync("Conta B", 0m);
        var incomeCategory = await CreateCategoryAsync("Outras receitas", TransactionType.Income);
        var expenseCategory = await CreateCategoryAsync("Outros gastos", TransactionType.Expense);

        // Um gasto na conta A e uma receita de mesmo valor na B, sem nada que indique transferência
        await CreateTransactionAsync(a.Id, 200m, TransactionType.Expense, new DateTime(2026, 2, 12),
            "Compra qualquer", expenseCategory);

        var previewB = await UploadAndProcessAsync(b.Id, BuildOfx("22222",
            new OfxRow(new DateTime(2026, 2, 12), 200m, "B1", "PIX RECEBIDO JOAO")));

        var row = Row(previewB, "PIX");
        row.TransferMatch.Should().NotBeNull();
        row.TransferMatch!.Status.Should().Be("Suggested");

        await ConfirmAsync(previewB.ImportId,
            new ConfirmImportItemDto(row.Id, true, incomeCategory, "Income", TransferAction: "Ignore"));

        var imported = (await ListTransactionsAsync()).Single(t => t.AccountId == b.Id);
        imported.Type.Should().Be(TransactionType.Income);
        imported.TransferLinkId.Should().BeNull();
    }
}
