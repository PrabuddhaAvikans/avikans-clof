using ATSolution.Domain.Entities.Common;

namespace Sales.Domain.Sequences;

public class DocumentSequence : Entity<Guid>
{
    public string DocumentType { get; private set; } = null!;
    public int Year { get; private set; }
    public int LastNumber { get; private set; }

    public static DocumentSequence Create(string documentType, int year, int lastNumber = 0)
    {
        return new DocumentSequence
        {
            Id = Guid.NewGuid(),
            DocumentType = documentType,
            Year = year,
            LastNumber = lastNumber,
        };
    }

    public int Next()
    {
        LastNumber += 1;
        return LastNumber;
    }
}
