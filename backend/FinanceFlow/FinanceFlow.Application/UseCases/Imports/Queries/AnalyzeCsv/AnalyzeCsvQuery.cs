using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.AnalyzeCsv;

/// <summary>
/// Lê o CSV e devolve as primeiras linhas e o que o mapeamento produz. Sem mapeamento (ou com um que não
/// serve para o arquivo) o sistema sugere um. Não grava nada.
/// </summary>
public record AnalyzeCsvQuery(
    Stream FileStream,
    CsvMappingDto? Mapping = null
) : IRequest<CsvAnalysisDto>;
