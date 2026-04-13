using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;

public class GetImportPreviewQueryHandler(
    IBankImportRepository bankImportRepository)
    : IRequestHandler<GetImportPreviewQuery, BankImportPreviewDto>
{
    public async Task<BankImportPreviewDto> Handle(
        GetImportPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("BankImport", request.ImportId);

        var transactions = bankImport.Transactions.Select(t => new BankImportTransactionDto(
            Id: t.Id,
            ExternalId: t.ExternalId,
            Date: t.Date,
            Amount: t.Amount,
            Description: t.Description,
            Type: t.Type,
            IsDuplicate: t.IsDuplicate,
            IsSelected: t.IsSelected,
            SuggestedCategoryId: t.SuggestedCategoryId,
            SuggestedCategoryName: t.SuggestedCategory?.Name
        ));

        return new BankImportPreviewDto(
            ImportId: bankImport.Id,
            FileName: bankImport.FileName,
            TotalRecords: bankImport.TotalRecords,
            Duplicates: bankImport.Duplicates,
            Transactions: transactions);
    }
}
