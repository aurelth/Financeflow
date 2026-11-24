using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Mappings;

/// <summary>
/// Cobre o mapeamento manual entidade → DTO (que substituiu o AutoMapper): campos copiados,
/// regras derivadas e o comportamento quando falta um dado que o repositório deveria ter carregado.
/// </summary>
public class EntityMappingsTests
{
    private static Category NewCategory(string name = "Mercado", Guid? userId = null) => new()
    {
        Name = name,
        Icon = "cart",
        Color = "#f59e0b",
        Type = TransactionType.Expense,
        IsDefault = userId is null,
        UserId = userId,
    };

    private static Transaction NewTransaction(Category? category = null) => new()
    {
        Amount = 150.75m,
        Type = TransactionType.Expense,
        Date = new DateTime(2026, 3, 10),
        Description = "Compra",
        Status = TransactionStatus.Paid,
        Tags = "[]",
        AccountId = Guid.NewGuid(),
        Account = new Account { Name = "Nubank" },
        CategoryId = category?.Id ?? Guid.NewGuid(),
        Category = category ?? NewCategory(),
    };

    // Budget

    [Fact]
    public void Budget_DeveCopiarOsCamposEOsDadosDaCategoria()
    {
        var category = NewCategory("Lazer");
        var budget = new Budget
        {
            CategoryId = category.Id, Category = category,
            Month = 3, Year = 2026, LimitAmount = 800m, UpdatedAt = new DateTime(2026, 3, 5),
        };

        var dto = budget.ToDto();

        dto.Id.Should().Be(budget.Id);
        dto.CategoryId.Should().Be(category.Id);
        dto.CategoryName.Should().Be("Lazer");
        dto.CategoryIcon.Should().Be("cart");
        dto.CategoryColor.Should().Be("#f59e0b");
        dto.Month.Should().Be(3);
        dto.Year.Should().Be(2026);
        dto.LimitAmount.Should().Be(800m);
        dto.CreatedAt.Should().Be(budget.CreatedAt);
        dto.UpdatedAt.Should().Be(new DateTime(2026, 3, 5));
    }

    [Fact]
    public void Budget_DeveFalharAlto_QuandoACategoriaNaoFoiCarregada()
    {
        var budget = new Budget { Category = null! };

        var act = () => budget.ToDto();

        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Budgets_DeveMaterializarALista()
    {
        var budgets = new[] { new Budget { Category = NewCategory() }, new Budget { Category = NewCategory() } };

        budgets.ToDtos().Should().HaveCount(2).And.BeOfType<List<FinanceFlow.Application.DTOs.BudgetDto>>();
    }

    // Category

    [Fact]
    public void Category_DeveMarcarComoDono_QuandoCriadaPeloUsuario()
    {
        NewCategory(userId: Guid.NewGuid()).ToDto().IsOwner.Should().BeTrue();
        NewCategory(userId: null).ToDto().IsOwner.Should().BeFalse();
    }

    [Fact]
    public void Category_DeveListarSoSubcategoriasAtivasENaoExcluidas_EmOrdemAlfabetica()
    {
        var category = NewCategory();
        category.Subcategories =
        [
            new Subcategory { Name = "Feira", IsActive = true },
            new Subcategory { Name = "Açougue", IsActive = true },
            new Subcategory { Name = "Inativa", IsActive = false },
            new Subcategory { Name = "Excluída", IsActive = true, DeletedAt = DateTime.UtcNow },
            new Subcategory { Name = "Padaria", IsActive = true },
        ];

        var dto = category.ToDto();

        dto.Subcategories.Select(s => s.Name).Should().Equal("Açougue", "Feira", "Padaria");
        dto.Subcategories.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public void Category_DeveCopiarOsCampos()
    {
        var category = NewCategory("Metas");
        category.IsGoalCategory = true;
        category.IsActive = false;

        var dto = category.ToDto();

        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be("Metas");
        dto.Icon.Should().Be("cart");
        dto.Color.Should().Be("#f59e0b");
        dto.Type.Should().Be(TransactionType.Expense);
        dto.IsDefault.Should().BeTrue();
        dto.IsActive.Should().BeFalse();
        dto.IsGoalCategory.Should().BeTrue();
    }

    // Notification

    [Fact]
    public void Notification_DeveCopiarOsCampos()
    {
        var notification = new Notification
        {
            Type = "BudgetCritical", Message = "Estourou", IsRead = true, ReferenceId = Guid.NewGuid(),
        };

        var dto = notification.ToDto();

        dto.Id.Should().Be(notification.Id);
        dto.Type.Should().Be("BudgetCritical");
        dto.Message.Should().Be("Estourou");
        dto.IsRead.Should().BeTrue();
        dto.ReferenceId.Should().Be(notification.ReferenceId);
        dto.CreatedAt.Should().Be(notification.CreatedAt);
    }

    // User

    [Fact]
    public void User_DeveConverterEnumsParaTexto_ExporOAvatar_ENuncaOHashDaSenha()
    {
        var user = new User
        {
            Name = "Aurel", Email = "a@a.com", Cpf = "12345678900", Gender = Gender.Female,
            Role = UserRole.Admin, Currency = "USD", Timezone = "America/New_York", Language = "en-US",
            AvatarPath = "avatars/a.png", PasswordHash = "segredo",
        };

        var dto = user.ToProfileDto();

        dto.Gender.Should().Be("Female");
        dto.Role.Should().Be("Admin");
        dto.AvatarUrl.Should().Be("avatars/a.png");
        dto.Name.Should().Be("Aurel");
        dto.Email.Should().Be("a@a.com");
        dto.Currency.Should().Be("USD");
        dto.Timezone.Should().Be("America/New_York");
        dto.Language.Should().Be("en-US");
        System.Text.Json.JsonSerializer.Serialize(dto).Should().NotContain("segredo");
    }

    // Transaction

    [Fact]
    public void Transaction_DeveCopiarOsCamposEOsDadosDeCategoriaESubcategoriaEConta()
    {
        var category = NewCategory("Mercado");
        var tx = NewTransaction(category);
        tx.SubcategoryId = Guid.NewGuid();
        tx.Subcategory = new Subcategory { Name = "Feira" };

        var dto = tx.ToDto();

        dto.Id.Should().Be(tx.Id);
        dto.Amount.Should().Be(150.75m);
        dto.CategoryId.Should().Be(category.Id);
        dto.CategoryName.Should().Be("Mercado");
        dto.CategoryIcon.Should().Be("cart");
        dto.CategoryColor.Should().Be("#f59e0b");
        dto.SubcategoryId.Should().Be(tx.SubcategoryId);
        dto.SubcategoryName.Should().Be("Feira");
        dto.AccountId.Should().Be(tx.AccountId);
        dto.AccountName.Should().Be("Nubank");
    }

    [Fact]
    public void Transaction_DeveDeixarNulos_SubcategoriaEContrapartida_QuandoNaoHa()
    {
        var dto = NewTransaction().ToDto();

        dto.SubcategoryName.Should().BeNull();
        dto.TransferLinkId.Should().BeNull();
        dto.CounterpartTransactionId.Should().BeNull();
        dto.CounterpartAccountId.Should().BeNull();
        dto.CounterpartAccountName.Should().BeNull();
    }

    [Fact]
    public void Transaction_DeveExporAOutraPonta_QuandoEhASaidaDaTransferencia()
    {
        var accountIn = new Account { Name = "Itaú" };
        var tIn = NewTransaction(); tIn.AccountId = accountIn.Id; tIn.Account = accountIn;
        var tOut = NewTransaction();
        tOut.TransferDirection = TransferDirection.Out;
        tOut.LinkAsOut = new TransferLink { OutTransactionId = tOut.Id, InTransactionId = tIn.Id, InTransaction = tIn };

        var dto = tOut.ToDto();

        dto.TransferDirection.Should().Be(TransferDirection.Out);
        dto.TransferLinkId.Should().Be(tOut.LinkAsOut.Id);
        dto.CounterpartTransactionId.Should().Be(tIn.Id);
        dto.CounterpartAccountId.Should().Be(accountIn.Id);
        dto.CounterpartAccountName.Should().Be("Itaú");
    }

    [Fact]
    public void Transaction_DeveExporAOutraPonta_QuandoEhAEntradaDaTransferencia()
    {
        var accountOut = new Account { Name = "Nubank" };
        var tOut = NewTransaction(); tOut.AccountId = accountOut.Id; tOut.Account = accountOut;
        var tIn = NewTransaction();
        tIn.TransferDirection = TransferDirection.In;
        tIn.LinkAsIn = new TransferLink { OutTransactionId = tOut.Id, InTransactionId = tIn.Id, OutTransaction = tOut };

        var dto = tIn.ToDto();

        dto.TransferDirection.Should().Be(TransferDirection.In);
        dto.TransferLinkId.Should().Be(tIn.LinkAsIn.Id);
        dto.CounterpartTransactionId.Should().Be(tOut.Id);
        dto.CounterpartAccountId.Should().Be(accountOut.Id);
        dto.CounterpartAccountName.Should().Be("Nubank");
    }

    [Theory]
    [InlineData("[\"viagem\",\"2026\"]", new[] { "viagem", "2026" })]
    [InlineData("[]", new string[0])]
    [InlineData("", new string[0])]
    [InlineData("   ", new string[0])]
    [InlineData("null", new string[0])]
    public void Transaction_DeveLerAsTags(string json, string[] esperado)
    {
        var tx = NewTransaction();
        tx.Tags = json;

        tx.ToDto().Tags.Should().Equal(esperado);
    }

    [Fact]
    public void Transaction_DeveFalharAlto_QuandoACategoriaNaoFoiCarregada()
    {
        var tx = NewTransaction();
        tx.Category = null!;

        var act = () => tx.ToDto();

        act.Should().Throw<NullReferenceException>();
    }
}
