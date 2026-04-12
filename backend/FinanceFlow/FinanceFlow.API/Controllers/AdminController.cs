using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Application.UseCases.Admin.Commands.CreateDefaultCategory;
using FinanceFlow.Application.UseCases.Admin.Commands.DeactivateUser;
using FinanceFlow.Application.UseCases.Admin.Commands.DeleteDefaultCategory;
using FinanceFlow.Application.UseCases.Admin.Commands.DemoteUser;
using FinanceFlow.Application.UseCases.Admin.Commands.PromoteUser;
using FinanceFlow.Application.UseCases.Admin.Commands.ReactivateUser;
using FinanceFlow.Application.UseCases.Admin.Commands.UpdateDefaultCategory;
using FinanceFlow.Application.UseCases.Admin.Queries.GetDefaultCategories;
using FinanceFlow.Application.UseCases.Admin.Queries.GetMetrics;
using FinanceFlow.Application.UseCases.Admin.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceFlow.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireAdmin")]
public class AdminController(IMediator mediator) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    // GET api/admin/users
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetUsersQuery(page, pageSize, search, isActive),
            cancellationToken);

        return Ok(result);
    }

    // PATCH api/admin/users/{id}/deactivate
    [HttpPatch("users/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeactivateUserCommand(id, GetUserId()),
            cancellationToken);

        return NoContent();
    }

    // PATCH api/admin/users/{id}/reactivate
    [HttpPatch("users/{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ReactivateUserCommand(id),
            cancellationToken);

        return NoContent();
    }

    // PATCH api/admin/users/{id}/promote
    [HttpPatch("users/{id:guid}/promote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromoteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new PromoteUserCommand(id),
            cancellationToken);

        return NoContent();
    }

    // PATCH api/admin/users/{id}/demote
    [HttpPatch("users/{id:guid}/demote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DemoteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DemoteUserCommand(id),
            cancellationToken);

        return NoContent();
    }

    // GET api/admin/categories
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefaultCategories(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDefaultCategoriesQuery(),
            cancellationToken);

        return Ok(result);
    }

    // POST api/admin/categories
    [HttpPost("categories")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDefaultCategory(
        [FromBody] CreateDefaultCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateDefaultCategoryCommand(
                request.Name,
                request.Icon,
                request.Color,
                request.Type),
            cancellationToken);

        return CreatedAtAction(nameof(GetDefaultCategories), new { id = result.Id }, result);
    }

    // PUT api/admin/categories/{id}
    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDefaultCategory(
        Guid id,
        [FromBody] UpdateDefaultCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateDefaultCategoryCommand(id, request.Name, request.Icon, request.Color),
            cancellationToken);

        return Ok(result);
    }

    // DELETE api/admin/categories/{id}
    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDefaultCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteDefaultCategoryCommand(id),
            cancellationToken);

        return NoContent();
    }

    // GET api/admin/metrics
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMetricsQuery(), cancellationToken);
        return Ok(result);
    }
}
