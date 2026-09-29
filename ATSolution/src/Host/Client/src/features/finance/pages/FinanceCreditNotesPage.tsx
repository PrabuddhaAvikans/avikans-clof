import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "@/components/feedback/toast";
import { ROUTES } from "@/app/config/routes";
import { PageContainer } from "@/components/layout/PageContainer";
import { PageHeader } from "@/components/feedback/PageHeader";
import { PageContent } from "@/components/feedback/PageStates";
import { CreditNoteListPanel } from "@/features/finance/components/CreditNoteListPanel";
import { CreditNoteDetailPanel } from "@/features/finance/components/CreditNoteDetailPanel";
import { ApplyCreditNoteModal } from "@/features/finance/components/ApplyCreditNoteModal";
import { workspaceGrid, workspaceGridCol, workspacePanelFill } from "@/lib/panelLayout";
import type { CreditNote } from "@/types/credit-note";
import type { Invoice } from "@/types/invoice";
import { type CreditNoteStatusValue } from "@/types/status";
import { httpCreditNoteService } from "@/services/http/httpCreditNoteService";
import { httpInvoiceService } from "@/services/http/httpInvoiceService";

export function FinanceCreditNotesPage() {
  const [creditNotes, setCreditNotes] = useState<CreditNote[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<CreditNoteStatusValue | "">("");

  const [applyOpen, setApplyOpen] = useState(false);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [creditPage, invoicePage] = await Promise.all([
        httpCreditNoteService.list({ page: 1, pageSize: 200 }),
        httpInvoiceService.list({ page: 1, pageSize: 200 }),
      ]);
      setCreditNotes(creditPage.items);
      setInvoices(invoicePage.items);
      setSelectedId((current) => current ?? creditPage.items[0]?.id ?? null);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load credit notes.");
      setCreditNotes([]);
      setInvoices([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const selectedCreditNote = useMemo(() => {
    return creditNotes.find((cn) => cn.id === selectedId) ?? null;
  }, [creditNotes, selectedId]);

  const handleApply = async (args: {
    creditNoteId: string;
    invoiceId: string;
    amount: number;
    note: string;
  }) => {
    const credit = creditNotes.find((c) => c.id === args.creditNoteId);
    const invoice = invoices.find((inv) => inv.id === args.invoiceId);
    if (!credit || !invoice) return;

    const maxAmount = Math.min(credit.remainingAmount, invoice.outstandingAmount);
    const applyAmount = Math.max(0, Math.min(args.amount, maxAmount));
    if (applyAmount <= 0) return;

    try {
      const updated = await httpCreditNoteService.apply(args.creditNoteId, {
        invoiceId: args.invoiceId,
        amount: applyAmount,
        note: args.note,
      });
      setCreditNotes((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
      const refreshedInvoice = await httpInvoiceService.getById(args.invoiceId);
      setInvoices((prev) =>
        prev.map((inv) => (inv.id === refreshedInvoice.id ? refreshedInvoice : inv)),
      );
      toast.success(`Applied ${applyAmount.toFixed(2)} to invoice ${invoice.invoiceNumber}.`);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to apply credit note.");
    }
  };

  return (
    <PageContainer maxWidth="full" className="py-3">
      <PageHeader
        title="Finance"
        description="Invoices and credit notes. Apply credit notes to reduce invoice outstanding balances."
        breadcrumbs={[{ label: "Finance", href: ROUTES.finance.invoices }]}
      />

      <PageContent isLoading={isLoading} error={error}>
        <div className={workspaceGrid}>
          <div className={workspaceGridCol + " min-h-[18rem] lg:col-span-3"}>
            <CreditNoteListPanel
              items={creditNotes}
              selectedId={selectedId}
              onSelect={setSelectedId}
              search={search}
              onSearchChange={(v) => {
                setSearch(v);
              }}
              statusFilter={statusFilter}
              onStatusFilterChange={setStatusFilter}
              className={workspacePanelFill}
            />
          </div>

          <div className={workspaceGridCol + " min-h-[24rem] lg:col-span-9"}>
            <CreditNoteDetailPanel
              creditNote={selectedCreditNote}
              onApplyClick={() => setApplyOpen(true)}
              disableApply={!selectedCreditNote || selectedCreditNote.remainingAmount <= 0}
              className={workspacePanelFill}
            />
          </div>
        </div>

        <ApplyCreditNoteModal
          open={applyOpen}
          onClose={() => setApplyOpen(false)}
          creditNote={selectedCreditNote}
          invoices={invoices}
          onApply={handleApply}
        />
      </PageContent>
    </PageContainer>
  );
}
