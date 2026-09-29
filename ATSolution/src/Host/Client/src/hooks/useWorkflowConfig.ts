import { useCallback, useEffect, useState } from "react";
import {
  loadWorkflowCatalog,
  reloadWorkflowCatalog,
  saveWorkflowCatalog,
  WORKFLOW_CATALOG_UPDATED_EVENT,
} from "@/lib/workflow";
import type { WorkflowCatalog } from "@/types/workflow";
import { httpWorkflowService } from "@/services/http/httpWorkflowService";

export function useWorkflowCatalog() {
  const [catalog, setCatalog] = useState(loadWorkflowCatalog);

  useEffect(() => {
    let cancelled = false;
    httpWorkflowService
      .getCatalog()
      .then((remote) => {
        if (cancelled) return;
        if (remote.definitions.length > 0 || remote.versions.length > 0) {
          setCatalog(saveWorkflowCatalog(remote));
        }
      })
      .catch(() => {
        // Keep local catalog fallback.
      });

    const onUpdated = () => setCatalog(loadWorkflowCatalog());
    const onStorage = () => setCatalog(reloadWorkflowCatalog());
    window.addEventListener(WORKFLOW_CATALOG_UPDATED_EVENT, onUpdated);
    window.addEventListener("storage", onStorage);
    return () => {
      cancelled = true;
      window.removeEventListener(WORKFLOW_CATALOG_UPDATED_EVENT, onUpdated);
      window.removeEventListener("storage", onStorage);
    };
  }, []);

  const save = useCallback((next: WorkflowCatalog) => {
    setCatalog(saveWorkflowCatalog(next));
  }, []);

  return { catalog, save, reload: () => setCatalog(reloadWorkflowCatalog()) };
}
