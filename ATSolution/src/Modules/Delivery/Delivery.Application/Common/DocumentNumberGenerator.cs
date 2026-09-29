using ATSolution.Application.Abstractions.Persistence;
using Delivery.Domain.Sequences;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;

namespace Delivery.Application.Common;

public static class DocumentNumberGenerator
{
    public static async Task<string> NextDeliveryNumberAsync(
        IRepository<DocumentSequence, Guid> sequences,
        CancellationToken cancellationToken)
    {
        var documentType = DocumentSequenceTypes.Delivery;
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
