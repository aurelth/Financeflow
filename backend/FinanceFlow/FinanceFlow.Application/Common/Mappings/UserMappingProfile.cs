using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserProfileDto>()
            .ForCtorParam(nameof(UserProfileDto.Gender),
                opt => opt.MapFrom(src => src.Gender.ToString()));
    }
}
