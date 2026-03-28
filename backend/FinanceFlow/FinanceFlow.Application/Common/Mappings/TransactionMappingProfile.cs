using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using System.Text.Json;

namespace FinanceFlow.Application.Common.Mappings;

public class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        // Transaction → TransactionDto
        CreateMap<Transaction, TransactionDto>()
            .ForCtorParam(nameof(TransactionDto.CategoryName),
                opt => opt.MapFrom(src => src.Category.Name))
            .ForCtorParam(nameof(TransactionDto.CategoryIcon),
                opt => opt.MapFrom(src => src.Category.Icon))
            .ForCtorParam(nameof(TransactionDto.CategoryColor),
                opt => opt.MapFrom(src => src.Category.Color))
            .ForCtorParam(nameof(TransactionDto.SubcategoryName),
                opt => opt.MapFrom(src =>
                    src.Subcategory != null ? src.Subcategory.Name : null))
            .ForCtorParam(nameof(TransactionDto.Tags),
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<string[]>(
                        src.Tags, (JsonSerializerOptions?)null) ?? Array.Empty<string>()));
    }
}
