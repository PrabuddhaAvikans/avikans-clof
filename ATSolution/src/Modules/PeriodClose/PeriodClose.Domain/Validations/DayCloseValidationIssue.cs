using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Validations;

public class DayCloseValidationIssue : Entity<Guid>
{
    public Guid BusinessPeriodId { get; private set; }
    public string ValidationCode { get; private set; } = null!;
    public string ValidationType { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public bool IsBlocking { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    public static DayCloseValidationIssue Create(
        Guid businessPeriodId,
        string validationCode,
        string validationType,
        string message,
        bool isBlocking,
        string? entityType = null,
        string? entityId = null)
    {
        return new DayCloseValidationIssue
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
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
