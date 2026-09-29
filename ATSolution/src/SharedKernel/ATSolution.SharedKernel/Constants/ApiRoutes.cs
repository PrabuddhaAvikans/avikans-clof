namespace ATSolution.SharedKernel.Constants;

public static class ApiRoutes
{
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
    }

    public static class Catalog
    {
        public const string Categories = "api/categories";
        public const string Brands = "api/brands";
        public const string Products = "api/products";
    }

    public static class Inventory
    {
        public const string Items = "api/inventory";
        public const string Movements = "api/inventory/movements";
        public const string Warehouses = "api/warehouses";
        public const string UnitsOfMeasure = "api/units-of-measure";
        public const string Reprocessing = "api/reprocessing";
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
    }

    public static class Manufacturing
    {
        public const string Jobs = "api/manufacturing/jobs";
        public const string ProductionTracking = "api/manufacturing/production-tracking";
    }

    public static class Delivery
    {
        public const string Base = "api/deliveries";
    }

    public static class Finance
    {
        public const string Invoices = "api/invoices";
        public const string CreditNotes = "api/credit-notes";
    }

    public static class PeriodClose
    {
        public const string Base = "api/period-close";
    }

    public static class Reporting
    {
        public const string Dashboard = "api/dashboard";
        public const string Reports = "api/reports";
    }

    public static class Audit
    {
        public const string Base = "api/audit-logs";
    }

    public static class Notifications
    {
        public const string Base = "api/notifications";
    }

    public static class Configuration
    {
        public const string SystemSettings = "api/system-settings";
        public const string Workflows = "api/workflows";
    }

    public static class Admin
    {
        public const string Seed = "api/admin/seed";
    }
}
