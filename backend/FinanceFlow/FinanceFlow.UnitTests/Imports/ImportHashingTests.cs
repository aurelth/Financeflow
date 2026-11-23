using FinanceFlow.Application.Services;
using FluentAssertions;

namespace FinanceFlow.UnitTests.Imports;

public class ImportHashingTests
{
    private static readonly DateTime Date = new(2026, 11, 10);

    [Fact]
    public void ComputeBaseHash_DeveSerDeterministico_EIgnorarCaixaEEspacos()
    {
        var a = ImportHashing.ComputeBaseHash(Date, -10m, "  padaria  ", "DEBIT");
        var b = ImportHashing.ComputeBaseHash(Date, -10m, "PADARIA", "DEBIT");

        a.Should().Be(b);
        a.Should().HaveLength(64);
    }

    [Fact]
    public void ComputeBaseHash_DeveDiferir_QuandoValorOuDataMudam()
    {
        var baseHash = ImportHashing.ComputeBaseHash(Date, -10m, "PADARIA", "DEBIT");

        ImportHashing.ComputeBaseHash(Date, -11m, "PADARIA", "DEBIT").Should().NotBe(baseHash);
        ImportHashing.ComputeBaseHash(Date.AddDays(1), -10m, "PADARIA", "DEBIT").Should().NotBe(baseHash);
    }

    // Compatibilidade: a 1ª ocorrência mantém o hash gravado em importações antigas
    [Fact]
    public void WithOccurrence_DeveManterHashOriginal_NaPrimeiraOcorrencia()
    {
        var baseHash = ImportHashing.ComputeBaseHash(Date, -10m, "PADARIA", "DEBIT");

        ImportHashing.WithOccurrence(baseHash, 1).Should().Be(baseHash);
    }

    [Fact]
    public void WithOccurrence_DeveGerarHashesDistintos_NasOcorrenciasSeguintes()
    {
        var baseHash = ImportHashing.ComputeBaseHash(Date, -10m, "PADARIA", "DEBIT");

        var segunda = ImportHashing.WithOccurrence(baseHash, 2);
        var terceira = ImportHashing.WithOccurrence(baseHash, 3);

        new[] { baseHash, segunda, terceira }.Distinct().Should().HaveCount(3);
        segunda.Should().HaveLength(64);
    }
}
