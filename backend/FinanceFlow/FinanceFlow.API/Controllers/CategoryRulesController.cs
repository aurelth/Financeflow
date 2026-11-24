using FinanceFlow.Application.DTOs.CategoryRules;
using FinanceFlow.Application.UseCases.CategoryRules.Commands.DeleteCategoryRule;
using FinanceFlow.Application.UseCases.CategoryRules.Queries.GetCategoryRules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

/// <summary>Regras "descrição → categoria" aprendidas nas importações do próprio usuário.</summary>
[Authorize]
public class CategoryRulesController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Lista as regras aprendidas, das mais confirmadas para as menos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCategoryRulesQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Esquece uma regra: as próximas importações voltam a usar apenas as palavras-chave.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCategoryRuleCommand(CurrentUserId, id), cancellationToken);
        return NoContent();
    }
}
