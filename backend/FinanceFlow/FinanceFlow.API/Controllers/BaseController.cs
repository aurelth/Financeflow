using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator => mediator;

    // Extrai o UserId do JWT token
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Utilizador não autenticado."));
}
