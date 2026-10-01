using ATSolution.SharedKernel.Models;

namespace Finance.Application.Invoices;

public sealed record RecordInvoicePaymentCommand(
    Guid Id,
    decimal Amount);
