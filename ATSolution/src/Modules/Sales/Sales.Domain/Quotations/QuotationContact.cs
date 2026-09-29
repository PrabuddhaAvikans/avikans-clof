using ATSolution.Domain.Entities.Common;

namespace Sales.Domain.Quotations;

public class QuotationContact : Entity<Guid>
{
    public Guid QuotationId { get; private set; }
    public Quotation Quotation { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Summary { get; private set; } = null!;
    public string? Detail { get; private set; }
    public string? Outcome { get; private set; }
    public string ContactedBy { get; private set; } = null!;
    public string ContactedByName { get; private set; } = null!;
    public DateTimeOffset ContactedAtUtc { get; private set; }

    public static QuotationContact Create(
        Guid quotationId,
        string type,
        string summary,
        string? detail,
        string? outcome,
        string contactedBy,
        string contactedByName,
        DateTimeOffset? contactedAtUtc = null)
    {
        return new QuotationContact
        {
            Id = Guid.NewGuid(),
            QuotationId = quotationId,
            Type = type,
            Summary = summary,
            Detail = detail,
            Outcome = outcome,
            ContactedBy = contactedBy,
            ContactedByName = contactedByName,
            ContactedAtUtc = contactedAtUtc ?? DateTimeOffset.UtcNow,
        };
    }
}
