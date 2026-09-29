namespace Finance.Domain.Common;

public static class InvoiceStatuses
{
    public const string Draft = "draft";
    public const string Issued = "issued";
    public const string Partial = "partial";
    public const string Paid = "paid";
    public const string Overdue = "overdue";
    public const string Void = "void";

    public static bool IsEditable(string status) => status == Draft;

    public static bool CanIssue(string status) => status == Draft;

    public static bool CanVoid(string status) =>
        status is Draft or Issued or Partial or Overdue;

    public static bool CanRecordPayment(string status) =>
        status is Issued or Partial or Overdue;
}

public static class CreditNoteStatuses
{
    public const string Draft = "draft";
    public const string Issued = "issued";
    public const string PartiallyApplied = "partially_applied";
    public const string Applied = "applied";
    public const string Void = "void";

    public static bool IsEditable(string status) => status == Draft;

    public static bool CanIssue(string status) => status == Draft;

    public static bool CanVoid(string status) =>
        status is Draft or Issued or PartiallyApplied;

    public static bool CanApply(string status) =>
        status is Issued or PartiallyApplied;
}

public static class CreditNoteReasons
{
    public const string Return = "return";
    public const string PriceAdjustment = "price_adjustment";
    public const string Overbilling = "overbilling";
    public const string DamagedGoods = "damaged_goods";
    public const string Goodwill = "goodwill";
    public const string Other = "other";

    public static readonly HashSet<string> All =
    [
        Return,
        PriceAdjustment,
        Overbilling,
        DamagedGoods,
        Goodwill,
        Other,
    ];
}

public static class DocumentSequenceTypes
{
    public const string Invoice = "INV";
    public const string CreditNote = "CN";
    public const string NumberFormat = "{0}-{1}-{2:D4}";
}

public static class FinanceDefaults
{
    public const string Currency = "LKR";
    public const string SystemActor = "system";
    public const string SystemActorName = "System";
}
