using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadCsv;

public class UploadCsvCommandHandler(IStatementImportService statementImportService)
    : IRequestHandler<UploadCsvCommand, BankImportDto>
{
    public async Task<BankImportDto> Handle(UploadCsvCommand request, CancellationToken cancellationToken)
    {
        var mapping = request.Mapping;
        var doc = CsvStatementParser.Read(request.FileStream, CsvStatementParser.ParseDelimiter(mapping.Delimiter));

        if (!CsvStatementParser.IsUsable(doc, mapping))
            throw new ValidationException("O mapeamento das colunas não corresponde a este arquivo. Refaça a análise.");

        var outcome = CsvStatementParser.Apply(doc, mapping);
        if (outcome.Rows.Count == 0)
            throw new ValidationException(
                "Nenhuma linha válida foi encontrada com este mapeamento. Confira as colunas, o formato da data e o separador decimal.");

        // Mesmo pipeline do OFX: conta, categorias sugeridas, deduplicação, preview e confirmação
        var result = await statementImportService.CreateAsync(
            request.UserId,
            request.FileName,
            request.AccountId,
            CsvStatementParser.ToParseResult(outcome),
            cancellationToken);

        return result with { SkippedRows = outcome.Errors.Count };
    }
}
