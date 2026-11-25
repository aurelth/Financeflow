using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.UseCases.Imports.Commands.ChangeImportAccount;
using FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;
using FinanceFlow.Application.UseCases.Imports.Commands.DeleteImport;
using FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;
using FinanceFlow.Application.UseCases.Imports.Commands.UploadCsv;
using FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;
using FinanceFlow.Application.UseCases.Imports.Queries.AnalyzeCsv;
using FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;
using FinanceFlow.Application.UseCases.Imports.Queries.GetImports;
using FinanceFlow.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class ImportsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Faz upload de um ficheiro OFX e inicia o processamento.</summary>
    [HttpPost("ofx")]
    [ProducesResponseType(typeof(BankImportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadOFX(
        IFormFile? file,
        [FromForm] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return UnprocessableEntity("Nenhum ficheiro enviado.");

        if (!file.FileName.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase))
            return UnprocessableEntity("Apenas ficheiros .ofx são suportados.");

        await using var stream = file.OpenReadStream();

        var command = new UploadOFXCommand(
            UserId: CurrentUserId,
            FileStream: stream,
            FileName: file.FileName,
            AccountId: accountId);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPreview), new { id = result.Id }, result);
    }

    // Margem para os campos do formulário além do arquivo
    private const long CsvRequestLimit = CsvStatementParser.MaxBytes + 64 * 1024;

    /// <summary>
    /// Lê um CSV e devolve as primeiras linhas, o mapeamento sugerido (ou o enviado) e o que ele produz.
    /// Não grava nada: o arquivo é reenviado no upload final, assim o servidor não guarda estado entre as etapas.
    /// </summary>
    [HttpPost("csv/analyze")]
    [RequestSizeLimit(CsvRequestLimit)]
    [ProducesResponseType(typeof(CsvAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnalyzeCsv(
        IFormFile? file,
        [FromForm] string? mapping,
        CancellationToken cancellationToken)
    {
        if (!IsValidCsvFile(file, out var error))
            return UnprocessableEntity(error);

        if (!TryReadMapping(mapping, required: false, out var parsed, out error))
            return UnprocessableEntity(error);

        await using var stream = file!.OpenReadStream();
        var result = await Mediator.Send(new AnalyzeCsvQuery(stream, parsed), cancellationToken);
        return Ok(result);
    }

    /// <summary>Faz upload de um CSV com o mapeamento das colunas e cria a importação (mesmo fluxo do OFX).</summary>
    [HttpPost("csv")]
    [RequestSizeLimit(CsvRequestLimit)]
    [ProducesResponseType(typeof(BankImportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadCsv(
        IFormFile? file,
        [FromForm] string? mapping,
        [FromForm] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (!IsValidCsvFile(file, out var error))
            return UnprocessableEntity(error);

        if (!TryReadMapping(mapping, required: true, out var parsed, out error))
            return UnprocessableEntity(error);

        await using var stream = file!.OpenReadStream();

        var command = new UploadCsvCommand(
            UserId: CurrentUserId,
            FileStream: stream,
            FileName: file.FileName,
            Mapping: parsed!,
            AccountId: accountId);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPreview), new { id = result.Id }, result);
    }

    private static bool IsValidCsvFile(IFormFile? file, out string error)
    {
        error = string.Empty;

        if (file is null || file.Length == 0)
            error = "Nenhum ficheiro enviado.";
        else if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                 && !file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            error = "Apenas ficheiros .csv são suportados.";
        else if (file.Length > CsvStatementParser.MaxBytes)
            error = "O arquivo é maior que 5 MB.";

        return error.Length == 0;
    }

    private static bool TryReadMapping(string? json, bool required, out CsvMappingDto? mapping, out string error)
    {
        mapping = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(json))
        {
            if (required)
                error = "Informe o mapeamento das colunas.";
            return !required;
        }

        try
        {
            mapping = JsonSerializer.Deserialize<CsvMappingDto>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            error = "Mapeamento de colunas inválido.";
            return false;
        }

        if (mapping is null && required)
            error = "Informe o mapeamento das colunas.";

        return error.Length == 0;
    }

    /// <summary>Retorna o preview de uma importação pendente.</summary>
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(typeof(BankImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetImportPreviewQuery(id, CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Troca a conta de uma importação ainda não confirmada e refaz a detecção de duplicatas.</summary>
    [HttpPut("{id:guid}/account")]
    [ProducesResponseType(typeof(BankImportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeAccount(
        Guid id,
        [FromBody] ChangeImportAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeImportAccountCommand(id, CurrentUserId, request.AccountId);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Confirma a importação das transações selecionadas.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(BankImportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] ConfirmImportRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmImportCommand(id, CurrentUserId, request);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Processa uma importação (uso interno do Worker).</summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Process(
        Guid id,
        [FromBody] ProcessImportRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessImportCommand(id, request.UserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Lista o histórico de importações do utilizador.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BankImportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetImportsQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Elimina uma importação e as suas transações de staging.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteImportCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
