using AutoMapper;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Categories.Queries.GetCategories;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Categories;

public class GetCategoriesQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetCategoriesQueryHandler CreateHandler() =>
        new(_categoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveRetornarCategorias_QuandoExistemCategorias()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categories = new[]
        {
            CategoryBuilder.Build(userId: userId),
            CategoryBuilder.Build(userId: userId, name: "Transporte"),
            CategoryBuilder.BuildDefault()
        };

        var query = new GetCategoriesQuery(userId);

        _categoryRepository
            .Setup(r => r.GetAllByUserAsync(userId, default))
            .ReturnsAsync(categories);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoExistemCategorias()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCategoriesQuery(userId);

        _categoryRepository
            .Setup(r => r.GetAllByUserAsync(userId, default))
            .ReturnsAsync([]);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
