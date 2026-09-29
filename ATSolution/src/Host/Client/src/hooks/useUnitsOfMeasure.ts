import { useCallback, useEffect, useState } from "react";
import {
  loadUnitsOfMeasure,
  UNITS_OF_MEASURE_UPDATED_EVENT,
  type UnitOfMeasure,
} from "@/lib/unitsOfMeasure";
import { httpUnitOfMeasureService } from "@/services/http/httpUnitOfMeasureService";

export function useUnitsOfMeasure(): UnitOfMeasure[] {
  const [units, setUnits] = useState(loadUnitsOfMeasure);

  const refresh = useCallback(() => {
    void httpUnitOfMeasureService
      .list({ page: 1, pageSize: 200 })
      .then((page) => {
        if (page.items.length > 0) {
          setUnits(page.items);
          return;
        }
        setUnits(loadUnitsOfMeasure());
      })
      .catch(() => setUnits(loadUnitsOfMeasure()));
  }, []);

  useEffect(() => {
    refresh();
    window.addEventListener(UNITS_OF_MEASURE_UPDATED_EVENT, refresh);
    window.addEventListener("storage", refresh);
    return () => {
      window.removeEventListener(UNITS_OF_MEASURE_UPDATED_EVENT, refresh);
      window.removeEventListener("storage", refresh);
    };
  }, [refresh]);

  return units;
}
