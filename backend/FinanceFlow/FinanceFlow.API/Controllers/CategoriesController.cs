using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Categories.Commands.CreateCategory;
using FinanceFlow.Application.UseCases.Categories.Commands.DeleteCategory;
using FinanceFlow.Application.UseCases.Categories.Commands.UpdateCategory;
using FinanceFlow.Application.UseCases.Categories.Queries.GetCategories;
using FinanceFlow.Application.UseCases.Categories.Queries.GetCategoryById;
using FinanceFlow.Application.UseCases.Subcategories.Commands.CreateSubcategory;
using FinanceFlow.Application.UseCases.Subcategories.Commands.DeleteSubcategory;
using FinanceFlow.Application.UseCases.Subcategories.Commands.UpdateSubcategory;
using FinanceFlow.Application.UseCases.Subcategories.Queries.GetSubcategoriesByCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class CategoriesController(IMediator mediator) : BaseController(mediator)
{
    // ─── Categorias ──────────────────────────────────────

    /// <summary>Lista todas as categorias visíveis para o utilizador.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetCategoriesQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna uma categoria pelo Id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id, CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria uma nova categoria.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(
            CurrentUserId,
            request.Name,
            request.Icon,
            request.Color,
            request.Type);

        var result = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    /// <summary>Atualiza uma categoria existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            id,
            CurrentUserId,
            request.Name,
            request.Icon,
            request.Color);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove uma categoria (soft delete se tiver transações).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    // ─── Subcategorias ───────────────────────────────────

    /// <summary>Lista todas as subcategorias de uma categoria.</summary>
    [HttpGet("{categoryId:guid}/subcategories")]
    [ProducesResponseType(typeof(IEnumerable<SubcategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubcategories(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var query = new GetSubcategoriesByCategoryQuery(categoryId, CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria uma subcategoria numa categoria.</summary>
    [HttpPost("{categoryId:guid}/subcategories")]
    [ProducesResponseType(typeof(SubcategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSubcategory(
        Guid categoryId,
        [FromBody] CreateSubcategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSubcategoryCommand(
            categoryId,
            CurrentUserId,
            request.Name);

        var result = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetSubcategories),
            new { categoryId },
            result);
    }

    /// <summary>Atualiza uma subcategoria.</summary>
    [HttpPut("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    [ProducesResponseType(typeof(SubcategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSubcategory(
        Guid categoryId,
        Guid subcategoryId,
        [FromBody] UpdateSubcategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSubcategoryCommand(
            subcategoryId,
            categoryId,
            CurrentUserId,
            request.Name);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove uma subcategoria (soft delete se tiver transações).</summary>
    [HttpDelete("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubcategory(
        Guid categoryId,
        Guid subcategoryId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteSubcategoryCommand(
            subcategoryId,
            categoryId,
            CurrentUserId);

        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
