using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;
using FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;
using FinanceFlow.Application.UseCases.Transactions.Commands.RemoveAttachment;
using FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactionById;
using FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactions;
using FinanceFlow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class TransactionsController(
    IMediator mediator,
    IAttachmentService attachmentService)
    : BaseController(mediator)
{
    /// <summary>Lista transações paginadas com filtros opcionais.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetTransactionsQueryDto filters,
        CancellationToken cancellationToken)
    {
        var query = new GetTransactionsQuery(
            UserId: CurrentUserId,
            Page: filters.Page,
            PageSize: filters.PageSize,
            DateFrom: filters.DateFrom,
            DateTo: filters.DateTo,
            CategoryId: filters.CategoryId,
            SubcategoryId: filters.SubcategoryId,
            Type: filters.Type,
            Status: filters.Status,
            AmountMin: filters.AmountMin,
            AmountMax: filters.AmountMax,
            Search: filters.Search);

        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna uma transação pelo Id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTransactionByIdQuery(id, CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria uma nova transação com anexo opcional via multipart/form-data.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromForm] string amount,
        [FromForm] int type,
        [FromForm] string date,
        [FromForm] string description,
        [FromForm] int status,
        [FromForm] bool isRecurring,
        [FromForm] int recurrenceType,
        [FromForm] string categoryId,
        [FromForm] string? subcategoryId,
        [FromForm] string[] tags,
        IFormFile? attachment,
        CancellationToken cancellationToken)
    {
        Stream? attachmentStream = null;
        string? attachmentFileName = null;
        string? attachmentContentType = null;

        if (attachment != null && attachment.Length > 0)
        {
            attachmentStream = attachment.OpenReadStream();
            attachmentFileName = attachment.FileName;
            attachmentContentType = attachment.ContentType;
        }

        var command = new CreateTransactionCommand(
            UserId: CurrentUserId,
            Amount: decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
            Type: (TransactionType)type,
            Date: DateTime.Parse(date, null, System.Globalization.DateTimeStyles.RoundtripKind),
            Description: description,
            Status: (TransactionStatus)status,
            IsRecurring: isRecurring,
            RecurrenceType: (RecurrenceType)recurrenceType,
            CategoryId: Guid.Parse(categoryId),
            SubcategoryId: subcategoryId != null ? Guid.Parse(subcategoryId) : null,
            Tags: tags,
            AttachmentStream: attachmentStream,
            AttachmentFileName: attachmentFileName,
            AttachmentContentType: attachmentContentType);

        var result = await Mediator.Send(command, cancellationToken);

        if (attachmentStream != null)
            await attachmentStream.DisposeAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza uma transação existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTransactionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTransactionCommand(
            Id: id,
            UserId: CurrentUserId,
            Amount: request.Amount,
            Type: request.Type,
            Date: request.Date,
            Description: request.Description,
            Status: request.Status,
            IsRecurring: request.IsRecurring,
            RecurrenceType: request.RecurrenceType,
            CategoryId: request.CategoryId,
            SubcategoryId: request.SubcategoryId,
            Tags: request.Tags);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove uma transação (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTransactionCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Faz upload ou substituição de anexo numa transação existente.</summary>
    [HttpPost("{id:guid}/attachment")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return UnprocessableEntity("Nenhum ficheiro enviado.");

        // Busca a transação para validar que pertence ao utilizador
        var query = new GetTransactionByIdQuery(id, CurrentUserId);
        var transaction = await Mediator.Send(query, cancellationToken);

        // Remove anexo anterior se existir
        if (!string.IsNullOrEmpty(transaction.AttachmentPath))
            await attachmentService.DeleteAsync(transaction.AttachmentPath);

        // Salva o novo anexo
        await using var stream = file.OpenReadStream();
        var attachmentPath = await attachmentService.SaveAsync(
            stream,
            file.FileName,
            file.ContentType,
            CurrentUserId,
            cancellationToken);

        // Atualiza a transação com o novo caminho
        var command = new UpdateTransactionCommand(
            Id: id,
            UserId: CurrentUserId,
            Amount: transaction.Amount,
            Type: transaction.Type,
            Date: transaction.Date,
            Description: transaction.Description,
            Status: transaction.Status,
            IsRecurring: transaction.IsRecurring,
            RecurrenceType: transaction.RecurrenceType,
            CategoryId: transaction.CategoryId,
            SubcategoryId: transaction.SubcategoryId,
            Tags: transaction.Tags);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove o anexo de uma transação.</summary>
    [HttpDelete("{id:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAttachment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RemoveAttachmentCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
