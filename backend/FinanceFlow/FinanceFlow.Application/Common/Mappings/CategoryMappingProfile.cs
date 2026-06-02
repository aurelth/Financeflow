using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        // Subcategory → SubcategoryDto
        CreateMap<Subcategory, SubcategoryDto>();

        // Category → CategoryDto        
        CreateMap<Category, CategoryDto>()
            .ForCtorParam(nameof(CategoryDto.IsOwner),
                opt => opt.MapFrom(src => src.UserId != null))
            .ForCtorParam(nameof(CategoryDto.IsGoalCategory),
                opt => opt.MapFrom(src => src.IsGoalCategory))
            .ForCtorParam(nameof(CategoryDto.Subcategories),
                opt => opt.MapFrom(src => src.Subcategories
                    .Where(s => s.DeletedAt == null && s.IsActive)
                    .OrderBy(s => s.Name)));
    }
}
