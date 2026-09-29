namespace Identity.Application.Permissions;

/// <summary>
/// Mirrors Host/Client/src/app/config/permissions.ts ALL_PERMISSIONS.
/// </summary>
public static class IdentityPermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, string[]> ModuleActions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["dashboard"] = ["view", "export"],
            ["products"] = ["view", "create", "edit", "delete", "export"],
            ["categories"] = ["view", "create", "edit", "delete"],
            ["inventory"] = ["view", "create", "edit", "delete", "export", "assign"],
            ["customers"] = ["view", "create", "edit", "delete", "export"],
            ["quotations"] = ["view", "create", "edit", "delete", "approve", "send", "export", "cancel"],
            ["sales_orders"] = ["view", "create", "edit", "delete", "approve", "export", "assign", "cancel"],
            ["finance"] = ["view", "create", "edit", "delete", "approve", "export", "cancel"],
            ["manufacturing"] = ["view", "create", "edit", "delete", "approve", "assign", "export", "cancel"],
            ["delivery"] = ["view", "create", "edit", "delete", "assign", "export", "cancel"],
            ["users"] = ["view", "create", "edit", "delete", "assign"],
            ["roles"] = ["view", "create", "edit", "delete", "assign"],
            ["settings"] = ["view", "edit"],
            ["audit_logs"] = ["view", "export"],
            ["reports"] = ["view", "export"],
            ["period_close"] = ["view", "create", "edit", "approve", "export"],
        };

    public static IReadOnlyList<(string Module, string Action, string Code)> All { get; } =
        ModuleActions
            .SelectMany(pair => pair.Value.Select(action => (pair.Key, action, $"{pair.Key}:{action}")))
            .ToList();
}
