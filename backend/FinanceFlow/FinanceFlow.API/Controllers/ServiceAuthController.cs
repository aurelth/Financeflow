using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class ServiceAuthController(
    ITokenService tokenService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Gera um JWT de serviço para workers internos.</summary>
    [HttpPost("service-token")]
    [ProducesResponseType(typeof(ServiceTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetServiceToken([FromBody] ServiceTokenRequestDto request)
    {
        var validKey = configuration["ServiceAuth:ServiceKey"];

        if (string.IsNullOrWhiteSpace(validKey) || request.ServiceKey != validKey)
            return Unauthorized("Chave de serviço inválida.");

        var serviceUserId = Guid.Parse(
            configuration["ServiceAuth:ServiceUserId"]
            ?? "00000000-0000-0000-0000-000000000001");

        var token = tokenService.GenerateServiceToken(serviceUserId, "BudgetWorker");

        return Ok(new ServiceTokenResponseDto(
            AccessToken: token.AccessToken,
            ExpiresAt: token.ExpiresAt));
    }
}
