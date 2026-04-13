using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;
using FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;
using FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;
using FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;
using FinanceFlow.Application.UseCases.Imports.Queries.GetImports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            FileName: file.FileName);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPreview), new { id = result.Id }, result);
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
}
