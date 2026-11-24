using FinanceFlow.Application.DTOs.QuickEntry;
using FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;
using FinanceFlow.Application.UseCases.QuickEntry.Queries.GetAiQuota;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

/// <summary>Lançamento por texto livre ou foto de comprovante, com IA.</summary>
[Authorize]
public class QuickEntryController(IMediator mediator) : BaseController(mediator)
{
    // Imagem de até 5 MB + o restante do formulário; o que passar disso é cortado antes de ser lido
    private const long MaxRequestBytes = 8 * 1024 * 1024;

    /// <summary>
    /// Lê um texto e/ou a foto de um comprovante e devolve rascunhos de lançamentos.
    /// Nada é gravado: o usuário confere e salva cada rascunho no formulário.
    /// </summary>
    [HttpPost("parse")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType(typeof(QuickEntryResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Parse(
        [FromForm] string? text,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        byte[]? bytes = null;
        if (image is { Length: > 0 })
        {
            using var buffer = new MemoryStream();
            await image.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }

        var result = await Mediator.Send(
            new ParseQuickEntryCommand(CurrentUserId, text, bytes), cancellationToken);

        return Ok(result);
    }

    /// <summary>Quantos lançamentos por IA o usuário ainda pode fazer hoje.</summary>
    [HttpGet("quota")]
    [ProducesResponseType(typeof(AiQuotaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuota(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAiQuotaQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }
}
