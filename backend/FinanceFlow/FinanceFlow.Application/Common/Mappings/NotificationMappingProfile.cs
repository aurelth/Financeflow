using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // Notification → NotificationDto
        CreateMap<Notification, NotificationDto>();
    }
}
