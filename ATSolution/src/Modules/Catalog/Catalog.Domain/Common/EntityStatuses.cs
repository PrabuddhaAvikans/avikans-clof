namespace Catalog.Domain.Common;

public static class EntityStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class ProductVersionStatuses
{
    public const string Draft = "draft";
    public const string Released = "released";
    public const string RevisionRequired = "revision_required";
    public const string Rejected = "rejected";
    public const string BomDefined = "bom_defined";

    public static bool IsImmutable(string status) =>
        status is Released;

    public static bool IsEditable(string status) =>
        status is Draft or BomDefined or RevisionRequired;
}
