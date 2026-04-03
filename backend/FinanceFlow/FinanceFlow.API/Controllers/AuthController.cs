using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Auth.Commands.ForgotPassword;
using FinanceFlow.Application.UseCases.Auth.Commands.LoginUser;
using FinanceFlow.Application.UseCases.Auth.Commands.Logout;
using FinanceFlow.Application.UseCases.Auth.Commands.RefreshToken;
using FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;
using FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

public class AuthController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Regista um novo utilizador.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Name,
            request.Email,
            request.Password,
            request.Currency,
            request.Timezone);

        var result = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(UsersController.GetProfile),
            "Users",
            new { },
            result);
    }

    /// <summary>Autentica um utilizador e retorna os tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);
        var result = await Mediator.Send(command, cancellationToken);

        // Armazena o Refresh Token em httpOnly cookie
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    /// <summary>Renova o Access Token usando o Refresh Token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] string accessToken,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized("Refresh Token não encontrado.");

        var command = new RefreshTokenCommand(accessToken, refreshToken);
        var result = await Mediator.Send(command, cancellationToken);

        // Renova o cookie com o novo Refresh Token
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    /// <summary>Termina a sessão do utilizador autenticado.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(CurrentUserId);
        await Mediator.Send(command, cancellationToken);

        // Remove o cookie do Refresh Token
        Response.Cookies.Delete("refreshToken");

        return NoContent();
    }

    /// <summary>Envia email de redefinição de senha.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Redefine a senha usando o token recebido por email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(
            request.Token,
            request.NewPassword,
            request.ConfirmPassword);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    // Helpers
    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,                            // não acessível via JavaScript
            Secure = true,                             // apenas via HTTPS
            SameSite = SameSiteMode.Strict,           // protecção CSRF
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
