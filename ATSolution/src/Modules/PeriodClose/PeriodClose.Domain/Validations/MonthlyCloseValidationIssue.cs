using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Validations;

public class MonthlyCloseValidationIssue : Entity<Guid>
{
    public Guid MonthlyPeriodId { get; private set; }
    public string ValidationCode { get; private set; } = null!;
    public string ValidationType { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public bool IsBlocking { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    public static MonthlyCloseValidationIssue Create(
        Guid monthlyPeriodId,
        string validationCode,
        string validationType,
        string message,
        bool isBlocking,
        string? entityType = null,
        string? entityId = null)
    {
        return new MonthlyCloseValidationIssue
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            ValidationCode = validationCode,
            ValidationType = validationType,
            Message = message,
            IsBlocking = isBlocking,
            EntityType = entityType,
            EntityId = entityId,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }
}
