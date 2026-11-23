using System.Security.Cryptography;
using System.Text;

namespace FinanceFlow.Application.Services;

/// <summary>
/// Hash de deduplicação das linhas do extrato.
/// A 1ª ocorrência mantém o hash antigo (compatível com lançamentos já importados);
/// ocorrências seguintes de linhas idênticas no mesmo arquivo recebem um sufixo,
/// de modo que dois cafés iguais no mesmo dia não sejam tratados como um só.
/// </summary>
public static class ImportHashing
{
    public static string ComputeBaseHash(DateTime date, decimal amount, string description, string type)
    {
        var raw = $"{date:yyyy-MM-dd}|{amount}|{description.Trim().ToUpperInvariant()}|{type}";
        return Sha256Hex(raw);
    }

    public static string WithOccurrence(string baseHash, int occurrence) =>
        occurrence <= 1 ? baseHash : Sha256Hex($"{baseHash}|{occurrence}");

    private static string Sha256Hex(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
}
