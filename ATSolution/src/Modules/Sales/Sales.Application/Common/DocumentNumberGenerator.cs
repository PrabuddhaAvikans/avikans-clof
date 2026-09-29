using ATSolution.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;
using Sales.Domain.Sequences;

namespace Sales.Application.Common;

public static class DocumentNumberGenerator
{
    public static async Task<string> NextAsync(
        IRepository<DocumentSequence, Guid> sequences,
        string documentType,
        CancellationToken cancellationToken)
    {
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
