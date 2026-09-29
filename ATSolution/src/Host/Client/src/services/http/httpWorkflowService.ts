import type {
  WorkflowCatalog,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowInstanceStep,
  WorkflowRule,
  WorkflowStepDefinition,
  WorkflowVersion,
} from "@/types/workflow";
import { apiRequest } from "@/services/apiClient";

function mapStep(raw: Record<string, unknown>): WorkflowStepDefinition {
  return {
    id: String(raw.id),
    stepOrder: Number(raw.stepOrder ?? 0),
    stepName: String(raw.stepName ?? ""),
    approvalRoleId: String(raw.approvalRoleId ?? ""),
    approvalRoleName: String(raw.approvalRoleName ?? ""),
    assigneeUserId: raw.assigneeUserId == null ? undefined : String(raw.assigneeUserId),
    assigneeName: raw.assigneeName as string | undefined,
    approvalType: (raw.approvalType as WorkflowStepDefinition["approvalType"]) ?? "sequential",
    minApprovals: Number(raw.minApprovals ?? 1),
  };
}

function mapDefinition(raw: Record<string, unknown>): WorkflowDefinition {
  return {
    id: String(raw.id),
    name: String(raw.name ?? ""),
    description: String(raw.description ?? ""),
    module: (raw.module as WorkflowDefinition["module"]) ?? "costing",
    isActive: Boolean(raw.isActive ?? true),
  };
}

function mapVersion(raw: Record<string, unknown>): WorkflowVersion {
  return {
    id: String(raw.id),
    workflowDefinitionId: String(raw.workflowDefinitionId),
    versionNumber: Number(raw.versionNumber ?? 1),
    status: (raw.status as WorkflowVersion["status"]) ?? "draft",
    isDefault: Boolean(raw.isDefault),
    effectiveFrom: raw.effectiveFrom as string | undefined,
    effectiveTo: raw.effectiveTo as string | undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    publishedAt: raw.publishedAt as string | undefined,
    steps: ((raw.steps as unknown[]) ?? []).map((item) =>
      mapStep(item as Record<string, unknown>),
    ),
  };
}

function mapRule(raw: Record<string, unknown>): WorkflowRule {
  return {
    id: String(raw.id),
    workflowDefinitionId: String(raw.workflowDefinitionId),
    name: String(raw.name ?? ""),
    priority: Number(raw.priority ?? 0),
    enabled: Boolean(raw.enabled ?? true),
    conditions: (raw.conditions as WorkflowRule["conditions"]) ?? [],
    workflowVersionId: String(raw.workflowVersionId),
  };
}

function mapCatalog(raw: Record<string, unknown>): WorkflowCatalog {
  return {
    definitions: ((raw.definitions as unknown[]) ?? []).map((item) =>
      mapDefinition(item as Record<string, unknown>),
    ),
    versions: ((raw.versions as unknown[]) ?? []).map((item) =>
      mapVersion(item as Record<string, unknown>),
    ),
    rules: ((raw.rules as unknown[]) ?? []).map((item) =>
      mapRule(item as Record<string, unknown>),
    ),
  };
}

function mapInstanceStep(raw: Record<string, unknown>): WorkflowInstanceStep {
  return {
    id: String(raw.id),
    stepDefinitionId: String(raw.stepDefinitionId ?? ""),
    stepOrder: Number(raw.stepOrder ?? 0),
    stepName: String(raw.stepName ?? ""),
    roleId: raw.roleId == null ? undefined : String(raw.roleId),
    roleName: String(raw.roleName ?? ""),
    assigneeUserId: raw.assigneeUserId == null ? undefined : String(raw.assigneeUserId),
    assigneeName: String(raw.assigneeName ?? ""),
    status: (raw.status as WorkflowInstanceStep["status"]) ?? "waiting",
    startedAt: raw.startedAt as string | undefined,
    approvedAt: raw.approvedAt as string | undefined,
    approvedBy: raw.approvedBy as string | undefined,
    remarks: raw.remarks as string | undefined,
  };
}

function mapInstance(raw: Record<string, unknown>): WorkflowInstance {
  return {
    id: String(raw.id),
    subjectType: (raw.subjectType as WorkflowInstance["subjectType"]) ?? "costing_request",
    subjectId: String(raw.subjectId ?? ""),
    workflowDefinitionId: String(raw.workflowDefinitionId),
    workflowDefinitionName: String(raw.workflowDefinitionName ?? ""),
    workflowVersionId: String(raw.workflowVersionId),
    workflowVersionNumber: Number(raw.workflowVersionNumber ?? 1),
    status: (raw.status as WorkflowInstance["status"]) ?? "not_started",
    currentStepOrder: Number(raw.currentStepOrder ?? 0),
    startedAt: String(raw.startedAt ?? new Date().toISOString()),
    completedAt: raw.completedAt as string | undefined,
    steps: ((raw.steps as unknown[]) ?? []).map((item) =>
      mapInstanceStep(item as Record<string, unknown>),
    ),
  };
}

export interface UpsertWorkflowDefinitionInput {
  id?: string;
  name: string;
  description: string;
  module?: string;
  isActive?: boolean;
  steps?: WorkflowStepDefinition[];
  rules?: Array<{
    id?: string;
    name: string;
    priority: number;
    enabled: boolean;
    conditions: WorkflowRule["conditions"];
    workflowVersionId?: string;
  }>;
}

export interface WorkflowService {
  getCatalog(): Promise<WorkflowCatalog>;
  upsertDefinition(data: UpsertWorkflowDefinitionInput): Promise<WorkflowDefinition>;
  publishVersion(id: string): Promise<WorkflowVersion>;
  listInstances(): Promise<WorkflowInstance[]>;
  getInstance(id: string): Promise<WorkflowInstance>;
}

export const httpWorkflowService: WorkflowService = {
  async getCatalog() {
    return mapCatalog(await apiRequest("/api/workflows/catalog"));
  },
  async upsertDefinition(data: UpsertWorkflowDefinitionInput) {
    return mapDefinition(
      await apiRequest("/api/workflows/definitions", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    );
  },
  async publishVersion(id: string) {
    return mapVersion(
      await apiRequest(`/api/workflows/versions/${id}/publish`, { method: "POST" }),
    );
  },
  async listInstances() {
    const raw = await apiRequest<unknown[]>("/api/workflows/instances");
    return (raw ?? []).map((item) => mapInstance(item as Record<string, unknown>));
  },
  async getInstance(id: string) {
    return mapInstance(await apiRequest(`/api/workflows/instances/${id}`));
  },
};
