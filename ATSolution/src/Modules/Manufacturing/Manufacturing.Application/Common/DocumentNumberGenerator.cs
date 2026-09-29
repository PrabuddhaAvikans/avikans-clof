using ATSolution.Application.Abstractions.Persistence;
using Manufacturing.Domain.Sequences;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;

namespace Manufacturing.Application.Common;

public static class DocumentNumberGenerator
{
    public static async Task<string> NextManufacturingJobNumberAsync(
        IRepository<DocumentSequence, Guid> sequences,
        CancellationToken cancellationToken)
    {
        var documentType = DocumentSequenceTypes.ManufacturingJob;
        var year = DateTimeOffset.UtcNow.Year;
        var sequence = await sequences.Query()
            .FirstOrDefaultAsync(x => x.DocumentType == documentType && x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = DocumentSequence.Create(documentType, year);
            await sequences.AddAsync(sequence, cancellationToken);
        }

        var next = sequence.Next();
        return string.Format(DocumentSequenceTypes.NumberFormat, documentType, year, next);
    }
}
