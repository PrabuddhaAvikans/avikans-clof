import { combineEpics } from "redux-observable";
import { createApiEpic } from "@/app/store/async/createApiEpic";
import { dashboardActions as actions } from "@/features/dashboard/store/dashboardSlice";
import { http } from "@/services/apiClient";
import { asRecord } from "@/services/mappers/common";
import { mapSummary } from "@/services/mappers/dashboardMappers";

const fetchSummaryEpic = createApiEpic({
  request: actions.fetchSummaryRequest,
  success: actions.fetchSummarySuccess,
  failure: actions.fetchSummaryFailure,
  execute: async () => mapSummary(asRecord(await http.get("/api/dashboard/summary"))),
});

export const dashboardEpic = combineEpics(fetchSummaryEpic);
