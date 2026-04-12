using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IBankImportRepository
{
    Task<BankImport?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankImport>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(BankImport bankImport, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankImport bankImport, CancellationToken cancellationToken = default);
    Task<bool> HashExistsAsync(Guid userId, string hash, CancellationToken cancellationToken = default);
}
