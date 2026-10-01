using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record PromoteCustomizationResultDto(QuotationDto Quotation, object Product);
