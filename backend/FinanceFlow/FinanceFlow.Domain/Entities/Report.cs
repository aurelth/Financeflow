using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public class Report : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ReportType Type { get; set; } = ReportType.CSV;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public int Month { get; set; }
    public int Year { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public DateTime? CompletedAt { get; set; }
}
