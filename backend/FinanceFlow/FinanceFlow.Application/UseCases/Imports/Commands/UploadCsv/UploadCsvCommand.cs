using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadCsv;

public record UploadCsvCommand(
    Guid UserId,
    Stream FileStream,
    string FileName,
    CsvMappingDto Mapping,
    Guid? AccountId = null
) : IRequest<BankImportDto>;
