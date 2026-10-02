namespace ATSolution.SharedKernel.Constants;

public static class ApiRoutes
{
    public const string ById = "{id:guid}";

    public static class Auth
    {
        public const string Base = "api/auth";
        public const string Login = "login";
    }

    public static class Identity
    {
        public const string Base = "api/identity";
        public const string EmailRoute = "{email}";
        public const string Users = "api/users";
        public const string Roles = "api/roles";
        public const string RoleGroups = "api/role-groups";
        public const string Permissions = "api/permissions";
        public const string ByModule = "by-module";
        public const string PermissionAssignment = ApiRoutes.ById + "/permission-assignment";
    }

    public static class Catalog
    {
        public const string Categories = "api/categories";
        public const string Brands = "api/brands";
        public const string Products = "api/products";
        public const string Tree = "tree";
        public const string ProductVersion = "{productId:guid}/versions/{versionId:guid}";
        public const string ReviseProductVersion = "{productId:guid}/versions/{sourceVersionId:guid}/revise";
        public const string ProductHeader = "{productId:guid}/header";
    }

    public static class Inventory
    {
        public const string Items = "api/inventory";
        public const string Movements = "api/inventory/movements";
        public const string Warehouses = "api/warehouses";
        public const string UnitsOfMeasure = "api/units-of-measure";
        public const string Reprocessing = "api/reprocessing";
        public const string LowStock = "low-stock";
        public const string BySku = "by-sku/{sku}";
        public const string PriceHistory = ApiRoutes.ById + "/price-history";
        public const string ItemMovements = ApiRoutes.ById + "/movements";
        public const string ScrapLots = "scrap-lots";
        public const string Start = ApiRoutes.ById + "/start";
        public const string Complete = ApiRoutes.ById + "/complete";
        public const string Cancel = ApiRoutes.ById + "/cancel";
    }

    public static class Customers
    {
        public const string Base = "api/customers";
    }

    public static class Sales
    {
        public const string Quotations = "api/quotations";
        public const string SalesOrders = "api/sales-orders";
        public const string Costing = "api/costing";
        public const string Send = ApiRoutes.ById + "/send";
        public const string ConvertToSalesOrder = ApiRoutes.ById + "/convert-to-sales-order";
        public const string Contacts = ApiRoutes.ById + "/contacts";
        public const string ApproveCustomization = "{quotationId:guid}/lines/{lineItemId:guid}/approve-customization";
        public const string PromoteCustomization = "{quotationId:guid}/lines/{lineItemId:guid}/promote-customization";
        public const string Confirm = ApiRoutes.ById + "/confirm";
        public const string Cancel = ApiRoutes.ById + "/cancel";
        public const string Assign = ApiRoutes.ById + "/assign";
        public const string BySalesOrder = "by-sales-order/{salesOrderId:guid}";
        public const string FromSalesOrderById = "from-sales-order/{salesOrderId:guid}";
        public const string SyncFromSalesOrderById = "sync-from-sales-order/{salesOrderId:guid}";
        public const string FromSalesOrder = "from-sales-order";
        public const string SyncFromSalesOrder = "sync-from-sales-order";
        public const string SubmitCoating = ApiRoutes.ById + "/submit-coating";
        public const string Approve = ApiRoutes.ById + "/approve";
        public const string Reject = ApiRoutes.ById + "/reject";
        public const string RequestChanges = ApiRoutes.ById + "/request-changes";
        public const string Notes = ApiRoutes.ById + "/notes";
        public const string Comments = ApiRoutes.ById + "/comments";
    }

    public static class Manufacturing
    {
        public const string Jobs = "api/manufacturing/jobs";
        public const string ProductionTracking = "api/manufacturing/production-tracking";
        public const string ReserveMaterials = ApiRoutes.ById + "/reserve-materials";
        public const string Start = ApiRoutes.ById + "/start";
        public const string Complete = ApiRoutes.ById + "/complete";
        public const string Hold = ApiRoutes.ById + "/hold";
        public const string TaskActions = ApiRoutes.ById + "/task-actions";
        public const string CompleteTasks = ApiRoutes.ById + "/complete-tasks";
        public const string Snapshot = "snapshot";
        public const string TrackingJobs = "jobs";
        public const string TrackingJobById = "jobs/" + ApiRoutes.ById;
        public const string TrackingStart = "start";
        public const string UpdateStage = "jobs/" + ApiRoutes.ById + "/update-stage";
        public const string TrackingHold = "jobs/" + ApiRoutes.ById + "/hold";
        public const string ReleaseToQc = "jobs/" + ApiRoutes.ById + "/release-to-qc";
    }

    public static class Delivery
    {
        public const string Base = "api/deliveries";
        public const string Status = ApiRoutes.ById + "/status";
        public const string Dispatch = ApiRoutes.ById + "/dispatch";
        public const string ProofOfDelivery = ApiRoutes.ById + "/proof-of-delivery";
    }

    public static class Finance
    {
        public const string Invoices = "api/invoices";
        public const string CreditNotes = "api/credit-notes";
        public const string Issue = ApiRoutes.ById + "/issue";
        public const string Void = ApiRoutes.ById + "/void";
        public const string Payments = ApiRoutes.ById + "/payments";
        public const string Apply = ApiRoutes.ById + "/apply";
    }

    public static class PeriodClose
    {
        public const string Base = "api/period-close";
        public const string Settings = "settings";
        public const string Days = "days";
        public const string Months = "months";
        public const string CurrentDay = "days/current";
        public const string DayById = "days/" + ApiRoutes.ById;
        public const string CurrentMonth = "months/current";
        public const string MonthById = "months/" + ApiRoutes.ById;
        public const string ValidateDay = DayById + "/validate";
        public const string CloseDay = DayById + "/close";
        public const string ReopenDay = DayById + "/reopen";
        public const string ValidateMonth = MonthById + "/validate";
        public const string CloseMonth = MonthById + "/close";
        public const string ReopenMonth = MonthById + "/reopen";
        public const string DayAudit = DayById + "/audit";
        public const string MonthAudit = MonthById + "/audit";
        public const string DaySummary = DayById + "/summary";
        public const string MonthSummary = MonthById + "/summary";
        public const string DayProductionSnapshots = DayById + "/production-snapshots";
        public const string MonthProductionSnapshots = MonthById + "/production-snapshots";
        public const string DayInventorySnapshots = DayById + "/inventory-snapshots";
        public const string MonthInventorySnapshots = MonthById + "/inventory-snapshots";
        public const string DaySessionCheckpoints = DayById + "/session-checkpoints";
        public const string Adjustments = "adjustments";
        public const string AssertWritable = "assert-writable";
    }

    public static class Reporting
    {
        public const string Dashboard = "api/dashboard";
        public const string Reports = "api/reports";
        public const string Summary = "summary";
        public const string OrderFlow = "order-flow";
        public const string ByReportId = "{reportId}";
    }

    public static class Audit
    {
        public const string Base = "api/audit-logs";
        public const string Summary = "summary";
    }

    public static class Notifications
    {
        public const string Base = "api/notifications";
        public const string UnreadCount = "unread-count";
        public const string Read = ApiRoutes.ById + "/read";
        public const string MarkAllRead = "mark-all-read";
    }

    public static class Configuration
    {
        public const string SystemSettings = "api/system-settings";
        public const string Workflows = "api/workflows";
        public const string Catalog = "catalog";
        public const string ResetCatalog = "catalog/reset";
        public const string Definitions = "definitions";
        public const string PublishVersion = "versions/" + ApiRoutes.ById + "/publish";
        public const string VersionDraft = "versions/" + ApiRoutes.ById + "/draft";
        public const string SaveVersion = "versions/" + ApiRoutes.ById + "/save";
        public const string ApplyVersion = "versions/" + ApiRoutes.ById + "/apply";
        public const string ActivateVersion = "versions/" + ApiRoutes.ById + "/activate";
        public const string DeleteVersion = "versions/" + ApiRoutes.ById;
        public const string Instances = "instances";
        public const string InstanceById = "instances/" + ApiRoutes.ById;
    }

    public static class Admin
    {
        public const string Seed = "api/admin/seed";
        public const string Status = "status";
    }
}
