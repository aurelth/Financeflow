using FinanceFlow.API.Hubs;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;
using FinanceFlow.Application.UseCases.Reports.Commands.UpdateReportStatus;
using FinanceFlow.Application.UseCases.Reports.Queries.GetReportDownload;
using FinanceFlow.Application.UseCases.Reports.Queries.GetReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class ReportsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Lista todos os relatórios do usuário.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetReportsQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Solicita a geração de um novo relatório CSV.</summary>
    [HttpPost("request")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Request(
        [FromBody] CreateReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RequestReportCommand(
            UserId: CurrentUserId,
            Month: request.Month,
            Year: request.Year);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    /// <summary>Faz download do arquivo CSV de um relatório concluído.</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetReportDownloadQuery(id, CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);

        if (!System.IO.File.Exists(result.FilePath))
            return NotFound("Arquivo não encontrado.");

        var stream = System.IO.File.OpenRead(result.FilePath);
        return File(stream, result.ContentType, result.FileName);
    }

    /// <summary>Atualiza o status de um relatório (uso interno do Worker).</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateReportStatusDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReportStatusCommand(id, request.Status, request.FilePath, request.FileName);
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>Notifica o usuário via SignalR quando o relatório estiver pronto.</summary>
    [HttpPost("notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Notify(
        [FromBody] NotifyReportDto request,
        [FromServices] IHubContext<ReportHub> hubContext,
        CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(request.UserId.ToString())
            .SendAsync("ReportReady", new
            {
                reportId = request.ReportId,
                fileName = request.FileName,
            }, cancellationToken);

        return Ok();
    }

    /// <summary>Solicita relatório mensalmente para um usuário específico (uso interno do Job).</summary>
    [HttpPost("request-internal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestInternal(
        [FromBody] RequestInternalReportDto request,
        CancellationToken cancellationToken)
    {
        var command = new RequestReportCommand(
            UserId: request.UserId,
            Month: request.Month,
            Year: request.Year);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }
}
