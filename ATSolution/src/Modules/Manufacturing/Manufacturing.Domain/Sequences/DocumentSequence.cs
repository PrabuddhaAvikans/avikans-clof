using ATSolution.Domain.Entities.Common;

namespace Manufacturing.Domain.Sequences;

public class DocumentSequence : Entity<Guid>
{
    public string DocumentType { get; private set; } = null!;
    public int Year { get; private set; }
    public int LastNumber { get; private set; }

    public static DocumentSequence Create(string documentType, int year) =>
        new()
        {
            Id = Guid.NewGuid(),
            DocumentType = documentType,
            Year = year,
            LastNumber = 0,
        };

    public int Next() => ++LastNumber;
}
