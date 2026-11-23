using System.Security.Cryptography;
using System.Text;
using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Infrastructure.Parsing;

/// <summary>
/// HMAC-SHA256 do identificador da conta do OFX. O "pepper" vem de Accounts:KeyPepper
/// e, se não configurado, de Jwt:Secret, para não exigir uma nova variável de ambiente.
/// Trocar o pepper invalida o reconhecimento automático (as contas continuam existindo
/// e podem ser escolhidas manualmente na importação).
/// </summary>
public class AccountKeyHasher(IConfiguration configuration) : IAccountKeyHasher
{
    public string ComputeKey(Guid userId, string? bankId, string accountId)
    {
        var pepper = configuration["Accounts:KeyPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
            pepper = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(pepper))
            pepper = "financeflow-account-key";

        var normalized = $"{userId:N}|{(bankId ?? string.Empty).Trim()}|{Normalize(accountId)}";
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(pepper),
            Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string Last4(string accountId)
    {
        var normalized = Normalize(accountId);
        return normalized.Length <= 4 ? normalized : normalized[^4..];
    }

    // Ignora espaços, pontos e hífens para que "12.345-6" e "123456" sejam a mesma conta
    private static string Normalize(string accountId) =>
        new(accountId.Where(char.IsLetterOrDigit).ToArray());
}
