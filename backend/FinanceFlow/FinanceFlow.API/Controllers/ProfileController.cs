using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Users.Commands.DeleteAvatar;
using FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;
using FinanceFlow.Application.UseCases.Users.Commands.UploadAvatar;
using FinanceFlow.Application.UseCases.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class ProfileController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna o perfil do utilizador autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserProfileQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Atualiza as preferências do perfil.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var command = request with { UserId = CurrentUserId };
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Faz upload do avatar do utilizador.</summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(UploadAvatarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();

        var command = new UploadAvatarCommand(
            UserId: CurrentUserId,
            FileStream: stream,
            ContentType: file.ContentType);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove o avatar do utilizador.</summary>
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAvatarCommand(CurrentUserId), cancellationToken);
        return NoContent();
    }
}
