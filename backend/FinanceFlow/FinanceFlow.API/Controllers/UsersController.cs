using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;
using FinanceFlow.Application.UseCases.Users.Queries.GetUserProfile;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class UsersController(
    IMediator mediator,
    IUserRepository userRepository) : BaseController(mediator)
{
    /// <summary>Retorna o perfil do usuário autenticado.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var query = new GetUserProfileQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Atualiza o perfil do usuário autenticado.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand(
            CurrentUserId,
            request.Name,
            request.Currency,
            request.Timezone);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna IDs de todos os usuários (uso interno do Worker).</summary>
    [HttpGet("internal")]
    [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllIds(CancellationToken cancellationToken)
    {
        var ids = await userRepository.GetAllIdsAsync(cancellationToken);
        return Ok(ids);
    }
}
