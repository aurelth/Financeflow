using FinanceFlow.Application.Services;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Installments;

public class InstallmentPlannerTests
{
    [Fact]
    public void Split_DeveDividirExatamente_QuandoDivisivel()
    {
        InstallmentPlanner.Split(1200m, 12).Should().OnlyContain(p => p == 100m).And.HaveCount(12);
    }

    [Fact]
    public void Split_DeveColocarOsCentavosQueSobramNasPrimeirasParcelas()
    {
        // 100,00 em 3x: 33,34 + 33,33 + 33,33
        InstallmentPlanner.Split(100m, 3).Should().Equal(33.34m, 33.33m, 33.33m);
    }

    [Fact]
    public void Split_DeveDistribuirUmCentavoPorParcela_QuandoSobramVarios()
    {
        // 100,00 em 7x = 10000 centavos: 1428 por parcela e sobram 4 → 4 parcelas de 14,29 e 3 de 14,28 (soma 100,00)
        var parcels = InstallmentPlanner.Split(100m, 7);

        parcels.Take(4).Should().OnlyContain(p => p == 14.29m);
        parcels.Skip(4).Should().OnlyContain(p => p == 14.28m);
        parcels.Sum().Should().Be(100m);
    }

    [Theory]
    [InlineData(0.01, 1)]
    [InlineData(0.10, 7)]
    [InlineData(99.99, 2)]
    [InlineData(1234.56, 12)]
    [InlineData(1000000.01, 60)]
    [InlineData(0.07, 7)]
    public void Split_ASomaDasParcelasEhSempreOTotal(double total, int count)
    {
        var parcels = InstallmentPlanner.Split((decimal)total, count);

        parcels.Should().HaveCount(count);
        parcels.Sum().Should().Be((decimal)total);
        parcels.Should().OnlyContain(p => p >= 0.01m);
        // Nenhuma parcela difere da outra por mais de um centavo
        (parcels.Max() - parcels.Min()).Should().BeLessThanOrEqualTo(0.01m);
    }

    [Fact]
    public void Split_DeveArredondarOTotalParaCentavos()
    {
        // 10,005 vira 10,01 (arredondamento comercial) antes de dividir
        InstallmentPlanner.Split(10.005m, 2).Sum().Should().Be(10.01m);
    }

    [Fact]
    public void Split_DeveRecusarNumeroDeParcelasInvalido()
    {
        var act = () => InstallmentPlanner.Split(100m, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DueDate_PrimeiraParcelaEhADataOriginal()
    {
        var first = new DateTime(2026, 3, 15, 10, 30, 0);
        InstallmentPlanner.DueDate(first, 1).Should().Be(first);
    }

    [Fact]
    public void DueDate_AvancaUmMesPorParcela_VirandoOAno()
    {
        var first = new DateTime(2026, 11, 10);

        InstallmentPlanner.DueDate(first, 2).Should().Be(new DateTime(2026, 12, 10));
        InstallmentPlanner.DueDate(first, 3).Should().Be(new DateTime(2027, 1, 10));
        InstallmentPlanner.DueDate(first, 14).Should().Be(new DateTime(2027, 12, 10));
    }

    [Fact]
    public void DueDate_NaoDeixaODiaEscorregar_AposMesCurto()
    {
        // 31/01 → 28/02 → 31/03: o dia volta a 31 quando o mês comporta (conta da data original)
        var first = new DateTime(2027, 1, 31);

        InstallmentPlanner.DueDate(first, 2).Should().Be(new DateTime(2027, 2, 28));
        InstallmentPlanner.DueDate(first, 3).Should().Be(new DateTime(2027, 3, 31));
        InstallmentPlanner.DueDate(first, 4).Should().Be(new DateTime(2027, 4, 30));
    }

    [Fact]
    public void DueDate_RespeitaAnoBissexto()
    {
        InstallmentPlanner.DueDate(new DateTime(2028, 1, 30), 2).Should().Be(new DateTime(2028, 2, 29));
    }
}
