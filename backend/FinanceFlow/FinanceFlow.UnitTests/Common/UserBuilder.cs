using FinanceFlow.Domain.Entities;

namespace FinanceFlow.UnitTests.Common;

public static class UserBuilder
{
    public static User Build(
        Guid? id = null,
        string name = "Aurel Lossou",
        string email = "aurel@teste.com",
        string passwordHash = "$2a$12$hashedpassword",
        string currency = "BRL",
        string timezone = "America/Sao_Paulo") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Currency = currency,
            Timezone = timezone,
            CreatedAt = DateTime.UtcNow
        };
}
