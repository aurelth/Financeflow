using FinanceFlow.Domain.Interfaces;

namespace FinanceFlow.Infrastructure.Auth;

public class PasswordService : IPasswordService
{
    // Cost factor 12 — balanço entre segurança e performance
    private const int CostFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, CostFactor);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
