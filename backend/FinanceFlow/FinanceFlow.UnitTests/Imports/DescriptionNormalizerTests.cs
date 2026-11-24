using FinanceFlow.Application.Services;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Imports;

public class DescriptionNormalizerTests
{
    [Theory]
    [InlineData("COMPRA NO DEBITO 12/11 MERCADO EXTRA 1234", "mercado extra")]
    [InlineData("Compra Cartão 03/12 Mercado Extra", "mercado extra")]
    [InlineData("MERCADO EXTRA", "mercado extra")]
    [InlineData("IFOOD*RESTAURANTE", "ifood restaurante")]
    [InlineData("UBER *TRIP 8F3A21", "uber trip")]
    [InlineData("PIX ENVIADO JOÃO DA SILVA", "joao silva")]
    [InlineData("Transferência recebida pelo Pix - Maria Souza", "maria souza")]
    [InlineData("PAGAMENTO BOLETO 23793.38128 ENERGIA ELETRICA", "energia eletrica")]
    [InlineData("PADARIA DO ZE LTDA", "padaria ze")]
    public void ToKey_DeveReduzirADescricaoAIdentidade(string description, string expected) =>
        DescriptionNormalizer.ToKey(description).Should().Be(expected);

    [Fact]
    public void ToKey_DeveManterMarcaComNomeNumerico() =>
        DescriptionNormalizer.ToKey("99 POP 1234").Should().Be("99 pop");

    [Fact]
    public void ToKey_DeveIgnorarDataEParcela_ParaMesmoEstabelecimentoGerarMesmaChave()
    {
        var a = DescriptionNormalizer.ToKey("NETFLIX.COM 05/11 PARC 01/12");
        var b = DescriptionNormalizer.ToKey("NETFLIX.COM 05/12 PARC 02/12");

        a.Should().Be(b).And.Be("netflix"); // ".com" some junto com a preposição "com"
    }

    [Fact]
    public void ToKey_DeveLimitarAQuantidadeDePalavras()
    {
        var key = DescriptionNormalizer.ToKey("LOJA ALFA BETA GAMA DELTA EPSILON ZETA");

        key.Should().Be("loja alfa beta gama delta");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PIX ENVIADO")]
    [InlineData("COMPRA NO DEBITO 12/11 1234")]
    public void ToKey_DeveRetornarVazio_QuandoNaoIdentificaNinguem(string? description) =>
        DescriptionNormalizer.ToKey(description).Should().BeEmpty();

    [Fact]
    public void ToKey_NuncaDeveExcederOLimiteDaColuna()
    {
        var longWord = new string('a', 300);

        DescriptionNormalizer.ToKey(longWord).Length.Should().BeLessThanOrEqualTo(120);
    }
}
