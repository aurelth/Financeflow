using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.AnalyzeCsv;

public class AnalyzeCsvQueryHandler : IRequestHandler<AnalyzeCsvQuery, CsvAnalysisDto>
{
    private const int MaxReportedErrors = 10;

    public Task<CsvAnalysisDto> Handle(AnalyzeCsvQuery request, CancellationToken cancellationToken)
    {
        var requested = request.Mapping;
        var doc = CsvStatementParser.Read(request.FileStream, CsvStatementParser.ParseDelimiter(requested?.Delimiter));

        // O usuário pode ter trocado separador ou cabeçalho: se os índices antigos não servem mais, sugere de novo
        var mapping = requested is not null && CsvStatementParser.IsUsable(doc, requested)
            ? requested with { Delimiter = doc.Delimiter.ToString() }
            : CsvStatementParser.Suggest(doc, requested?.HasHeader);

        var outcome = CsvStatementParser.Apply(doc, mapping);
        var columns = doc.Columns;

        string[] Pad(string[] row) => [.. row, .. Enumerable.Repeat(string.Empty, columns - row.Length)];

        var headers = mapping.HasHeader ? Pad(doc.Rows[0]) : [];
        var sample = doc.Rows
            .Skip(mapping.HasHeader ? 1 : 0)
            .Take(CsvStatementParser.SampleSize)
            .Select(r => (IReadOnlyList<string>)Pad(r))
            .ToList();

        return Task.FromResult(new CsvAnalysisDto(
            Encoding: doc.Encoding,
            Columns: columns,
            Headers: headers,
            SampleRows: sample,
            TotalRows: outcome.TotalRows,
            Mapping: mapping,
            ValidRows: outcome.Rows.Count,
            InvalidRows: outcome.Errors.Count,
            Preview: outcome.Rows.Take(CsvStatementParser.PreviewSize).Select(CsvStatementParser.ToPreview).ToList(),
            Errors: outcome.Errors.Take(MaxReportedErrors).ToList()));
    }
}
