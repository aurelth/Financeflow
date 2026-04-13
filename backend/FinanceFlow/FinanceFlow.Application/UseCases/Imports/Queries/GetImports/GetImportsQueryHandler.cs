using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.GetImports;

public class GetImportsQueryHandler(
    IBankImportRepository bankImportRepository)
    : IRequestHandler<GetImportsQuery, IEnumerable<BankImportDto>>
{
    public async Task<IEnumerable<BankImportDto>> Handle(
        GetImportsQuery request,
        CancellationToken cancellationToken)
    {
        var imports = await bankImportRepository.GetAllByUserAsync(
            request.UserId, cancellationToken);

        return imports.Select(b => new BankImportDto(
            Id: b.Id,
            FileName: b.FileName,
            Status: b.Status,
            TotalRecords: b.TotalRecords,
            Imported: b.Imported,
            Duplicates: b.Duplicates,
            Errors: b.Errors,
            ErrorMessage: b.ErrorMessage,
            CreatedAt: b.CreatedAt));
    }
}
