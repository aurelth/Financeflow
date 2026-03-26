using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Categories.Queries.GetCategoryById;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Categories;

public class GetCategoryByIdQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    public GetCategoryByIdQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetCategoryByIdQueryHandler CreateHandler() =>
        new(_categoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveRetornarCategoria_QuandoCategoriaExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var query = new GetCategoryByIdQuery(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);
        result.Name.Should().Be(category.Name);
        result.IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveMapearIsOwnerFalse_QuandoCategoriaEhPadrao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.BuildDefault();
        var query = new GetCategoryByIdQuery(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.IsOwner.Should().BeFalse();
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var query = new GetCategoryByIdQuery(categoryId, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync((Domain.Entities.Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(query, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{categoryId}*");
    }
}
