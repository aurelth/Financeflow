namespace FinanceFlow.Application.Services;

/// <summary>"Hoje" no fuso do usuário: às 22h em São Paulo já é amanhã em UTC, e uma conta que vence hoje não pode virar vencida.</summary>
public static class UserClock
{
    public static DateTime LocalToday(string? timezoneId, DateTimeOffset utcNow)
    {
        if (!string.IsNullOrWhiteSpace(timezoneId))
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return TimeZoneInfo.ConvertTime(utcNow, zone).Date;
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Fuso desconhecido neste servidor: cai para UTC em vez de falhar a tela inteira
            }
        }

        return utcNow.UtcDateTime.Date;
    }
}
