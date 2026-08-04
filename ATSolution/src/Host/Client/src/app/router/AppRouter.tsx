import type { ReactNode } from "react";
import { Navigate, Route, Routes, useParams } from "react-router-dom";
import { ROUTES } from "@/app/config/routes";
import type { Permission } from "@/app/config/permissions";
import { AppShell } from "@/components/layout/AppShell";
import { RequirePermission } from "@/components/layout/RequirePermission";
import {
  AuditLogsPage,
  NotificationSettingsPage,
  PermissionsPage,
  RoleGroupsPage,
  RolesPage,
  SystemSettingsPage,
  UserFormPage,
  UsersPage,
} from "@/features/admin/pages/AdminPages";
import {
  CustomerActivityPage,
  CustomerDetailPage,
  CustomerFormPage,
  CustomerGroupsPage,
  CustomerListPage,
} from "@/features/customers/pages/CustomerPages";
import { DashboardPage } from "@/features/dashboard/pages/DashboardPage";
import {
  DeliveryCalendarPage,
  DeliveryDetailPage,
  DeliveryFormPage,
  DeliveryListPage,
  DispatchPage,
  ProofOfDeliveryPage,
} from "@/features/delivery/pages/DeliveryPages";
import {
  InventoryFormPage,
  InventoryListPage,
  LowStockPage,
  StockMovementsPage,
  StockOverviewPage,
  UnitsOfMeasurePage,
} from "@/features/inventory/pages/InventoryPages";
import {
  ManufacturingJobDetailPage,
  ManufacturingJobFormPage,
  ManufacturingJobsPage,
  MaterialRequirementsPage,
  ProductionBoardPage,
  ProductionTrackingPage,
  QualityInspectionPage,
  ReadyToShipPage,
  WorkOrdersPage,
} from "@/features/manufacturing/pages/ManufacturingPages";
import {
  BrandsPage,
  CategoriesPage,
  PriceListsPage,
  ProductAttributesPage,
  ProductDetailPage,
  ProductFormPage,
  ProductListPage,
} from "@/features/products/pages/ProductPages";
import {
  EstimateFormPage,
  QuotationPreviewPage,
  QuotationWorkspacePage,
  SalesOrderDetailPage,
  SalesOrderFormPage,
  SalesOrderWorkspacePage,
  SalesOrderReviewPage,
  InvoicesPage,
  PaymentsPage,
} from "@/features/sales/pages/SalesPages";
import { CostingApprovalWorkspacePage } from "@/features/costing/pages/CostingApprovalWorkspacePage";
import { NotFoundPage } from "@/features/shared/pages/NotFoundPage";

type ProtectedRouteProps = {
  permission?: Permission;
  children: ReactNode;
};

function ProtectedRoute({ permission, children }: ProtectedRouteProps) {
  return <RequirePermission permission={permission}>{children}</RequirePermission>;
}

function LegacyEstimateRedirect({ mode }: { mode?: "edit" | "preview" }) {
  const { id } = useParams<{ id: string }>();
  if (!id) return <Navigate to={ROUTES.quotations.list} replace />;
  if (mode === "edit") return <Navigate to={ROUTES.quotations.edit(id)} replace />;
  if (mode === "preview") return <Navigate to={ROUTES.quotations.preview(id)} replace />;
  return <Navigate to={ROUTES.quotations.detail(id)} replace />;
}

export function AppRouter() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<Navigate to={ROUTES.dashboard} replace />} />

        <Route
          path={ROUTES.dashboard.slice(1)}
          element={
            <ProtectedRoute permission="dashboard:view">
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        {/* Quotations */}
        <Route path="sales" element={<Navigate to={ROUTES.quotations.list} replace />} />
        <Route
          path="quotations"
          element={
            <ProtectedRoute permission="quotations:view">
              <QuotationWorkspacePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="quotations/new"
          element={
            <ProtectedRoute permission="quotations:create">
              <EstimateFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="quotations/:id/preview"
          element={
            <ProtectedRoute permission="quotations:view">
              <QuotationPreviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="quotations/:id/edit"
          element={
            <ProtectedRoute permission="quotations:edit">
              <EstimateFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="quotations/:id"
          element={
            <ProtectedRoute permission="quotations:view">
              <QuotationPreviewPage />
            </ProtectedRoute>
          }
        />

        {/* Legacy estimate routes → quotations */}
        <Route path="estimates" element={<Navigate to={ROUTES.quotations.list} replace />} />
        <Route path="estimates/new" element={<Navigate to={ROUTES.quotations.new} replace />} />
        <Route path="estimates/:id/preview" element={<LegacyEstimateRedirect mode="preview" />} />
        <Route path="estimates/:id/edit" element={<LegacyEstimateRedirect mode="edit" />} />
        <Route path="estimates/:id" element={<LegacyEstimateRedirect />} />

        {/* Costing & Approval */}
        <Route
          path="costing/approval"
          element={
            <ProtectedRoute permission="quotations:view">
              <CostingApprovalWorkspacePage />
            </ProtectedRoute>
          }
        />

        {/* Sales Orders */}
        <Route
          path="sales-orders"
          element={
            <ProtectedRoute permission="sales_orders:view">
              <SalesOrderWorkspacePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales-orders/new"
          element={
            <ProtectedRoute permission="sales_orders:create">
              <SalesOrderFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales-orders/:id/review"
          element={
            <ProtectedRoute permission="sales_orders:edit">
              <SalesOrderReviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales-orders/:id/edit"
          element={
            <ProtectedRoute permission="sales_orders:edit">
              <SalesOrderFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales-orders/:id"
          element={
            <ProtectedRoute permission="sales_orders:view">
              <SalesOrderDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Legacy sales routes */}
        <Route
          path="sales/estimates"
          element={<Navigate to={ROUTES.quotations.list} replace />}
        />
        <Route
          path="sales/quotations"
          element={<Navigate to={ROUTES.quotations.list} replace />}
        />
        <Route
          path="sales/orders"
          element={<Navigate to={ROUTES.salesOrders.list} replace />}
        />
        <Route
          path="sales/payments"
          element={
            <ProtectedRoute permission="sales_orders:view">
              <PaymentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales/invoices"
          element={
            <ProtectedRoute permission="sales_orders:view">
              <InvoicesPage />
            </ProtectedRoute>
          }
        />

        {/* Customers */}
        <Route
          path="customers"
          element={
            <ProtectedRoute permission="customers:view">
              <CustomerListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="customers/new"
          element={
            <ProtectedRoute permission="customers:create">
              <CustomerFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="customers/create"
          element={<Navigate to={ROUTES.customers.new} replace />}
        />
        <Route
          path="customers/groups"
          element={
            <ProtectedRoute permission="customers:view">
              <CustomerGroupsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="customers/activity"
          element={
            <ProtectedRoute permission="customers:view">
              <CustomerActivityPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="customers/:id/edit"
          element={
            <ProtectedRoute permission="customers:edit">
              <CustomerFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="customers/:id"
          element={
            <ProtectedRoute permission="customers:view">
              <CustomerDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Products — static paths before :id */}
        <Route
          path="products"
          element={
            <ProtectedRoute permission="products:view">
              <ProductListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/new"
          element={
            <ProtectedRoute permission="products:create">
              <ProductFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/create"
          element={<Navigate to={ROUTES.products.new} replace />}
        />
        <Route
          path="products/categories"
          element={
            <ProtectedRoute permission="categories:view">
              <CategoriesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/brands"
          element={
            <ProtectedRoute permission="products:view">
              <BrandsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/attributes"
          element={
            <ProtectedRoute permission="products:view">
              <ProductAttributesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/price-lists"
          element={
            <ProtectedRoute permission="products:view">
              <PriceListsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/:id/edit"
          element={
            <ProtectedRoute permission="products:edit">
              <ProductFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="products/:id"
          element={
            <ProtectedRoute permission="products:view">
              <ProductDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Inventory — static paths before :id */}
        <Route
          path="inventory"
          element={
            <ProtectedRoute permission="inventory:view">
              <InventoryListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/new"
          element={
            <ProtectedRoute permission="inventory:create">
              <InventoryFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/items"
          element={<Navigate to={ROUTES.inventory.list} replace />}
        />
        <Route
          path="inventory/stock"
          element={
            <ProtectedRoute permission="inventory:view">
              <StockOverviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/movements"
          element={
            <ProtectedRoute permission="inventory:view">
              <StockMovementsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/low-stock"
          element={
            <ProtectedRoute permission="inventory:view">
              <LowStockPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/units"
          element={
            <ProtectedRoute permission="inventory:view">
              <UnitsOfMeasurePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/:id/edit"
          element={
            <ProtectedRoute permission="inventory:edit">
              <InventoryFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="inventory/:id"
          element={
            <ProtectedRoute permission="inventory:view">
              <InventoryFormPage />
            </ProtectedRoute>
          }
        />

        {/* Manufacturing / Production */}
        <Route
          path="manufacturing"
          element={<Navigate to={ROUTES.manufacturing.tracking} replace />}
        />
        <Route
          path="manufacturing/tracking"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <ProductionTrackingPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/ready-to-ship"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <ReadyToShipPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/jobs/new"
          element={
            <ProtectedRoute permission="manufacturing:create">
              <ManufacturingJobFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/jobs/:id"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <ManufacturingJobDetailPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/jobs"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <ManufacturingJobsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/work-orders"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <WorkOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/board"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <ProductionBoardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/material-requirements"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <MaterialRequirementsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="manufacturing/quality"
          element={
            <ProtectedRoute permission="manufacturing:view">
              <QualityInspectionPage />
            </ProtectedRoute>
          }
        />

        {/* Deliveries */}
        <Route
          path="deliveries"
          element={
            <ProtectedRoute permission="delivery:view">
              <DeliveryListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="deliveries/new"
          element={
            <ProtectedRoute permission="delivery:create">
              <DeliveryFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="deliveries/schedule"
          element={<Navigate to={ROUTES.deliveries.calendar} replace />}
        />
        <Route
          path="deliveries/calendar"
          element={
            <ProtectedRoute permission="delivery:view">
              <DeliveryCalendarPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="deliveries/:id/dispatch"
          element={
            <ProtectedRoute permission="delivery:edit">
              <DispatchPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="deliveries/:id/proof"
          element={
            <ProtectedRoute permission="delivery:edit">
              <ProofOfDeliveryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="deliveries/:id"
          element={
            <ProtectedRoute permission="delivery:view">
              <DeliveryDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Legacy delivery routes */}
        <Route path="delivery" element={<Navigate to={ROUTES.deliveries.list} replace />} />
        <Route
          path="delivery/schedule"
          element={<Navigate to={ROUTES.deliveries.calendar} replace />}
        />
        <Route
          path="delivery/dispatch/:id"
          element={
            <ProtectedRoute permission="delivery:edit">
              <DispatchPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="delivery/dispatch"
          element={
            <ProtectedRoute permission="delivery:view">
              <DeliveryListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="delivery/proof/:id"
          element={
            <ProtectedRoute permission="delivery:edit">
              <ProofOfDeliveryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="delivery/proof"
          element={
            <ProtectedRoute permission="delivery:view">
              <DeliveryListPage />
            </ProtectedRoute>
          }
        />

        {/* Admin */}
        <Route path="admin" element={<Navigate to={ROUTES.admin.users} replace />} />
        <Route
          path="admin/users/new"
          element={
            <ProtectedRoute permission="users:create">
              <UserFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/users/:id/edit"
          element={
            <ProtectedRoute permission="users:edit">
              <UserFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/users"
          element={
            <ProtectedRoute permission="users:view">
              <UsersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/roles"
          element={
            <ProtectedRoute permission="roles:view">
              <RolesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/role-groups"
          element={
            <ProtectedRoute permission="roles:view">
              <RoleGroupsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/permissions"
          element={
            <ProtectedRoute permission="roles:view">
              <PermissionsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/settings"
          element={
            <ProtectedRoute permission="settings:view">
              <SystemSettingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/notifications"
          element={
            <ProtectedRoute permission="settings:view">
              <NotificationSettingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/audit-logs"
          element={
            <ProtectedRoute permission="audit_logs:view">
              <AuditLogsPage />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
