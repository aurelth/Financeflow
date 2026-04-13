using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.GetImports;

public record GetImportsQuery(Guid UserId) : IRequest<IEnumerable<BankImportDto>>;
