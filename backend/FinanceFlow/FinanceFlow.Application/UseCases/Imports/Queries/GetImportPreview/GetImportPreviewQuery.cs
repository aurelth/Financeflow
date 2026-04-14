using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;

public record GetImportPreviewQuery(
    Guid ImportId,
    Guid UserId
) : IRequest<BankImportPreviewDto>;
