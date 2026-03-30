using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

public class BudgetMappingProfile : Profile
{
    public BudgetMappingProfile()
    {
        // Budget → BudgetDto
        CreateMap<Budget, BudgetDto>()
            .ForCtorParam(nameof(BudgetDto.CategoryName),
                opt => opt.MapFrom(src => src.Category.Name))
            .ForCtorParam(nameof(BudgetDto.CategoryIcon),
                opt => opt.MapFrom(src => src.Category.Icon))
            .ForCtorParam(nameof(BudgetDto.CategoryColor),
                opt => opt.MapFrom(src => src.Category.Color));
    }
}
