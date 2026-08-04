import { useMemo, useState } from "react";
import type { ColumnDef } from "@tanstack/react-table";
import { useCustomers } from "@/features/customers/hooks/useCustomers";
import { PageContent } from "@/components/feedback/PageStates";
import { DataTable } from "@/components/tables/DataTable";
import { SearchBar } from "@/components/ui/SearchBar";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import type { Customer } from "@/types/customer";
import { CustomerType } from "@/types/customer";

export type CustomerSelectorModalProps = {
  open: boolean;
  onClose: () => void;
  onSelect: (customer: Customer) => void;
  title?: string;
};

export function CustomerSelectorModal({
  open,
  onClose,
  onSelect,
  title = "Select Customer",
}: CustomerSelectorModalProps) {
  const [search, setSearch] = useState("");
  const { data, isLoading, error, refetch } = useCustomers({
    page: 1,
    pageSize: 50,
    search: search || undefined,
    status: "active",
  });

  const columns = useMemo<ColumnDef<Customer>[]>(
    () => [
      { accessorKey: "code", header: "Customer #" },
      { accessorKey: "name", header: "Name" },
      {
        accessorKey: "type",
        header: "Type",
        cell: ({ row }) =>
          row.original.type.charAt(0).toUpperCase() + row.original.type.slice(1),
      },
      { accessorKey: "email", header: "Email" },
      {
        id: "actions",
        header: "",
        cell: ({ row }) => (
          <Button
            size="sm"
            variant="outline"
            onClick={() => {
              onSelect(row.original);
              onClose();
            }}
          >
            Select
          </Button>
        ),
      },
    ],
    [onClose, onSelect],
  );

  return (
    <Modal open={open} onClose={onClose} title={title} size="xl">
      <div className="space-y-4">
        <SearchBar
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search customers..."
        />
        <PageContent
          isLoading={isLoading}
          error={error ? "Failed to load customers." : null}
          onRetry={() => void refetch()}
          isEmpty={!isLoading && !error && (data?.items.length ?? 0) === 0}
          emptyTitle="No customers found"
          loadingVariant="table"
        >
          <DataTable
            data={data?.items ?? []}
            columns={columns}
            pageSize={8}
            enableColumnVisibility={false}
            getRowId={(row) => row.id}
          />
        </PageContent>
      </div>
    </Modal>
  );
}

export const CUSTOMER_TYPE_OPTIONS = [
  { value: CustomerType.individual, label: "Individual" },
  { value: CustomerType.retail, label: "Retail" },
  { value: CustomerType.corporate, label: "Corporate" },
];
