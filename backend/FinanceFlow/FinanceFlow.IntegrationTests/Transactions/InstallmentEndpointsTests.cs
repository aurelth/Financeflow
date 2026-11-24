using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.IntegrationTests.Transactions;

/// <summary>Compras parceladas: criação, plano, edição/exclusão em grupo, cancelamento e reflexo no orçamento.</summary>
public class InstallmentEndpointsTests(FinanceFlowWebApplicationFactory factory)
    : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private static readonly DateTime FirstDate = new(2027, 3, 10);

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            Name: "Parcelas",
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

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name = "Eletrônicos Parcelados")
    {
        var response = await client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequestDto(name, "💻", "#6366f1", TransactionType.Expense));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadAsJsonAsync<CategoryDto>())!;
    }

    private static MultipartFormDataContent PurchaseForm(
        Guid categoryId, string amount = "300.00", int? installments = 3, int status = 1,
        bool recurring = false, int type = 2, DateTime? date = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(amount),                                        "amount"         },
            { new StringContent(type.ToString()),                               "type"           },
            { new StringContent((date ?? FirstDate).ToString("o")),             "date"           },
            { new StringContent("Notebook"),                                    "description"    },
            { new StringContent(status.ToString()),                             "status"         },
            { new StringContent(recurring.ToString().ToLower()),                "isRecurring"    },
            { new StringContent("0"),                                           "recurrenceType" },
            { new StringContent(categoryId.ToString()),                         "categoryId"     },
        };

        if (installments.HasValue)
            form.Add(new StringContent(installments.Value.ToString()), "installmentCount");

        return form;
    }

    private static async Task<TransactionDto> CreatePurchaseAsync(
        HttpClient client, Guid categoryId, string amount = "300.00", int installments = 3, int status = 1)
    {
        var response = await client.PostAsync("/api/transactions", PurchaseForm(categoryId, amount, installments, status));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadAsJsonAsync<TransactionDto>())!;
    }

    private static async Task<List<TransactionDto>> ListParcelsAsync(HttpClient client, Guid groupId)
    {
        var response = await client.GetAsync("/api/transactions?dateFrom=2027-01-01&dateTo=2030-12-31&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadAsJsonAsync<PagedResultDto<TransactionDto>>())!;
        return page.Items.Where(t => t.InstallmentGroupId == groupId).OrderBy(t => t.InstallmentNumber).ToList();
    }

    // Criação

    [Fact]
    public async Task Create_DeveGerarTodasAsParcelas_ComValoresDatasEStatus()
    {
        var client = await AuthenticatedClientAsync("parcelas.criar@teste.com");
        var category = await CreateCategoryAsync(client);

        var first = await CreatePurchaseAsync(client, category.Id, amount: "1000.00", installments: 3);

        first.InstallmentNumber.Should().Be(1);
        first.InstallmentCount.Should().Be(3);
        first.InstallmentGroupId.Should().NotBeNull();

        var parcels = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);

        parcels.Select(p => p.InstallmentNumber).Should().Equal(1, 2, 3);
        parcels.Select(p => p.Amount).Should().Equal(333.34m, 333.33m, 333.33m);
        parcels.Sum(p => p.Amount).Should().Be(1000m);
        parcels.Select(p => p.Date.Date).Should().Equal(new DateTime(2027, 3, 10), new DateTime(2027, 4, 10), new DateTime(2027, 5, 10));
        parcels[0].Status.Should().Be(TransactionStatus.Paid);
        parcels.Skip(1).Should().OnlyContain(p => p.Status == TransactionStatus.Scheduled);
        parcels.Should().OnlyContain(p => !p.IsRecurring && p.RecurrenceGroupId == null && p.Description == "Notebook");
    }

    [Fact]
    public async Task Create_SemParcelas_ContinuaSendoLancamentoComum()
    {
        var client = await AuthenticatedClientAsync("parcelas.comum@teste.com");
        var category = await CreateCategoryAsync(client);

        var response = await client.PostAsync("/api/transactions", PurchaseForm(category.Id, "50.00", installments: null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadAsJsonAsync<TransactionDto>())!;
        created.InstallmentGroupId.Should().BeNull();
        created.InstallmentNumber.Should().BeNull();
        created.Amount.Should().Be(50m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(61)]
    public async Task Create_DeveRetornar422_QuandoONumeroDeParcelasEstaForaDoIntervalo(int installments)
    {
        var client = await AuthenticatedClientAsync($"parcelas.intervalo{installments}@teste.com");
        var category = await CreateCategoryAsync(client);

        var response = await client.PostAsync("/api/transactions", PurchaseForm(category.Id, installments: installments));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DeveRetornar422_QuandoParceladoERecorrente()
    {
        var client = await AuthenticatedClientAsync("parcelas.recorrente@teste.com");
        var category = await CreateCategoryAsync(client);

        var response = await client.PostAsync("/api/transactions", PurchaseForm(category.Id, recurring: true));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_NaoGravaParcelaNenhuma_QuandoOPedidoEInvalido()
    {
        var client = await AuthenticatedClientAsync("parcelas.atomico@teste.com");
        var category = await CreateCategoryAsync(client);

        // Total de R$ 0,05 em 12x: menos de um centavo por parcela
        var response = await client.PostAsync("/api/transactions", PurchaseForm(category.Id, "0.05", 12));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var page = (await (await client.GetAsync("/api/transactions?pageSize=100")).Content.ReadAsJsonAsync<PagedResultDto<TransactionDto>>())!;
        page.Items.Should().BeEmpty();
    }

    // Plano

    [Fact]
    public async Task Plano_DeveRetornarParcelasETotais()
    {
        var client = await AuthenticatedClientAsync("parcelas.plano@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id, "300.00", 3);

        var response = await client.GetAsync($"/api/transactions/installments/{first.InstallmentGroupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = (await response.Content.ReadAsJsonAsync<InstallmentPlanDto>())!;
        plan.Count.Should().Be(3);
        plan.TotalAmount.Should().Be(300m);
        plan.PaidAmount.Should().Be(100m);
        plan.RemainingAmount.Should().Be(200m);
        plan.Installments.Should().HaveCount(3);
    }

    [Fact]
    public async Task Plano_DeveRetornar404_ParaGrupoInexistente()
    {
        var client = await AuthenticatedClientAsync("parcelas.plano404@teste.com");

        var response = await client.GetAsync($"/api/transactions/installments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Plano_DeveRetornar401_SemToken()
    {
        var response = await factory.CreateClient().GetAsync($"/api/transactions/installments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Edição em grupo

    [Fact]
    public async Task Update_ComPropagate_AlteraAsParcelasSeguintesENaoAsAnteriores()
    {
        var client = await AuthenticatedClientAsync("parcelas.propagar@teste.com");
        var category = await CreateCategoryAsync(client);
        var other = await CreateCategoryAsync(client, "Casa Parcelada");
        var first = await CreatePurchaseAsync(client, category.Id, "600.00", 6);
        var parcels = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        var third = parcels[2];

        var response = await client.PutAsJsonAsync($"/api/transactions/{third.Id}", new UpdateTransactionRequestDto(
            Amount: 120m, Type: TransactionType.Expense, Date: third.Date, Description: "Notebook (renegociado)",
            Status: third.Status, IsRecurring: false, RecurrenceType: RecurrenceType.None,
            CategoryId: other.Id, SubcategoryId: null, Tags: [], PropagateToFuture: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var after = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        after.Take(2).Should().OnlyContain(p => p.Amount == 100m && p.Description == "Notebook" && p.CategoryId == category.Id);
        after.Skip(2).Should().OnlyContain(p => p.Amount == 120m && p.Description == "Notebook (renegociado)" && p.CategoryId == other.Id);
    }

    [Fact]
    public async Task Update_NaoPropagaParaParcelasJaPagas()
    {
        var client = await AuthenticatedClientAsync("parcelas.propagar.pagas@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id, "400.00", 4);
        var parcels = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);

        // A parcela 3 já foi paga (adiantada)
        var paid = await client.PutAsJsonAsync($"/api/transactions/{parcels[2].Id}", new UpdateTransactionRequestDto(
            parcels[2].Amount, TransactionType.Expense, parcels[2].Date, "Notebook", TransactionStatus.Paid,
            false, RecurrenceType.None, category.Id, null, []));
        paid.EnsureSuccessStatusCode();

        // Alterar o valor a partir da parcela 2
        var response = await client.PutAsJsonAsync($"/api/transactions/{parcels[1].Id}", new UpdateTransactionRequestDto(
            150m, TransactionType.Expense, parcels[1].Date, "Notebook", TransactionStatus.Scheduled,
            false, RecurrenceType.None, category.Id, null, [], PropagateToFuture: true));
        response.EnsureSuccessStatusCode();

        var after = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        after[1].Amount.Should().Be(150m);
        after[2].Amount.Should().Be(100m, "a parcela 3 já estava paga e não muda em massa");
        after[3].Amount.Should().Be(150m);
    }

    [Fact]
    public async Task Update_DeveRetornar422_AoTornarUmaParcelaRecorrente()
    {
        var client = await AuthenticatedClientAsync("parcelas.upd.recorrente@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id);

        var response = await client.PutAsJsonAsync($"/api/transactions/{first.Id}", new UpdateTransactionRequestDto(
            100m, TransactionType.Expense, first.Date, "Notebook", TransactionStatus.Paid,
            IsRecurring: true, RecurrenceType.Monthly, category.Id, null, []));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // Exclusão e cancelamento

    [Fact]
    public async Task Delete_ComDeleteFuture_RemoveAParcelaEAsSeguintesNaoPagas()
    {
        var client = await AuthenticatedClientAsync("parcelas.delete.futuras@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id, "500.00", 5);
        var parcels = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);

        var response = await client.DeleteAsync($"/api/transactions/{parcels[2].Id}?deleteFuture=true");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        after.Select(p => p.InstallmentNumber).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Delete_SemDeleteFuture_RemoveSoAParcelaEscolhida()
    {
        var client = await AuthenticatedClientAsync("parcelas.delete.uma@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id, "400.00", 4);
        var parcels = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);

        (await client.DeleteAsync($"/api/transactions/{parcels[1].Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        after.Select(p => p.InstallmentNumber).Should().Equal(1, 3, 4);
    }

    [Fact]
    public async Task Cancelar_RemoveAsNaoPagas_EMantemAPrimeiraPaga()
    {
        var client = await AuthenticatedClientAsync("parcelas.cancelar@teste.com");
        var category = await CreateCategoryAsync(client);
        var first = await CreatePurchaseAsync(client, category.Id, "300.00", 3);

        var response = await client.DeleteAsync($"/api/transactions/installments/{first.InstallmentGroupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadAsJsonAsync<CancelInstallmentsResultDto>())!;
        (result.CancelledCount, result.KeptPaidCount).Should().Be((2, 1));

        var after = await ListParcelsAsync(client, first.InstallmentGroupId!.Value);
        after.Should().ContainSingle().Which.InstallmentNumber.Should().Be(1);
    }

    [Fact]
    public async Task Cancelar_DeveRetornar404_ParaGrupoDeOutroUsuario_ESemApagarNada()
    {
        var owner = await AuthenticatedClientAsync("parcelas.dono@teste.com");
        var intruder = await AuthenticatedClientAsync("parcelas.intruso@teste.com");
        var category = await CreateCategoryAsync(owner);
        var first = await CreatePurchaseAsync(owner, category.Id, "300.00", 3);

        (await intruder.DeleteAsync($"/api/transactions/installments/{first.InstallmentGroupId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetAsync($"/api/transactions/installments/{first.InstallmentGroupId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await ListParcelsAsync(owner, first.InstallmentGroupId!.Value)).Should().HaveCount(3);
    }

    // Reflexo nos orçamentos e no saldo

    [Fact]
    public async Task Orcamento_DosMesesSeguintes_CountaAParcelaDoMes()
    {
        var client = await AuthenticatedClientAsync("parcelas.orcamento@teste.com");
        var category = await CreateCategoryAsync(client);
        await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequestDto(category.Id, Month: 4, Year: 2027, LimitAmount: 1000m));

        await CreatePurchaseAsync(client, category.Id, "300.00", 3);

        // A parcela 2 vence em abril/2027
        var response = await client.GetAsync("/api/budgets/summary?month=4&year=2027");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await response.Content.ReadAsJsonAsync<IEnumerable<BudgetSummaryDto>>())!.Single();
        summary.SpentAmount.Should().Be(100m);
    }
}
