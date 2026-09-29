import { useCallback, useEffect, useState } from "react";
import {
  loadWarehouses,
  WAREHOUSES_UPDATED_EVENT,
  type Warehouse,
} from "@/lib/warehouses";
import { httpWarehouseService } from "@/services/http/httpWarehouseService";

export function useWarehouses(): Warehouse[] {
  const [warehouses, setWarehouses] = useState(loadWarehouses);

  const refresh = useCallback(() => {
    void httpWarehouseService
      .list({ page: 1, pageSize: 200 })
      .then((page) => {
        if (page.items.length > 0) {
          setWarehouses(page.items);
          return;
        }
        setWarehouses(loadWarehouses());
      })
      .catch(() => setWarehouses(loadWarehouses()));
  }, []);

  useEffect(() => {
    refresh();
    window.addEventListener(WAREHOUSES_UPDATED_EVENT, refresh);
    window.addEventListener("storage", refresh);
    return () => {
      window.removeEventListener(WAREHOUSES_UPDATED_EVENT, refresh);
      window.removeEventListener("storage", refresh);
    };
  }, [refresh]);

  return warehouses;
}
