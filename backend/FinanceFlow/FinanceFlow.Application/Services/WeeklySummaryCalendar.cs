using System.Security.Cryptography;
using System.Text;

namespace FinanceFlow.Application.Services;

/// <summary>Regras de calendário do resumo semanal: qual semana resumir e quando ela deve sair.</summary>
public static class WeeklySummaryCalendar
{
    /// <summary>Hora local (do usuário) a partir da qual o resumo da segunda-feira pode sair.</summary>
    public const int DeliveryHour = 8;

    /// <summary>Segunda-feira da última semana fechada: quando hoje é segunda, é a segunda de 7 dias atrás.</summary>
    public static DateTime PreviousWeekStart(DateTime today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.Date.AddDays(-daysSinceMonday - 7);
    }

    /// <summary>O resumo sai na segunda-feira, depois das 8h no fuso do usuário.</summary>
    public static bool IsDeliveryWindow(DateTime localNow) =>
        localNow.DayOfWeek == DayOfWeek.Monday && localNow.Hour >= DeliveryHour;

    /// <summary>Id determinístico por usuário e semana: o mesmo resumo nunca vira duas notificações no mesmo dia.</summary>
    public static Guid ReferenceId(Guid userId, DateTime weekStart)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"weekly-summary:{userId:N}:{weekStart:yyyyMMdd}"));
        return new Guid(bytes);
    }
}
