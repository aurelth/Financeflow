using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

/// <summary>Mapeamento Notification → NotificationDto.</summary>
public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification) => new(
        Id:          notification.Id,
        Type:        notification.Type,
        Message:     notification.Message,
        IsRead:      notification.IsRead,
        ReferenceId: notification.ReferenceId,
        CreatedAt:   notification.CreatedAt);

    public static List<NotificationDto> ToDtos(this IEnumerable<Notification> notifications) =>
        notifications.Select(ToDto).ToList();
}
