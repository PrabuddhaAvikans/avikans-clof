import type {
  WorkflowCatalog,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowInstanceStep,
  WorkflowRule,
  WorkflowStepDefinition,
  WorkflowVersion,
} from "@/types/workflow";

export function mapStep(raw: Record<string, unknown>): WorkflowStepDefinition {
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

export function mapDefinition(raw: Record<string, unknown>): WorkflowDefinition {
  return {
    id: String(raw.id),
    name: String(raw.name ?? ""),
    description: String(raw.description ?? ""),
    module: (raw.module as WorkflowDefinition["module"]) ?? "costing",
    isActive: Boolean(raw.isActive ?? true),
  };
}

export function mapVersion(raw: Record<string, unknown>): WorkflowVersion {
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

export function mapRule(raw: Record<string, unknown>): WorkflowRule {
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

export function mapCatalog(raw: Record<string, unknown>): WorkflowCatalog {
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

export function mapInstanceStep(raw: Record<string, unknown>): WorkflowInstanceStep {
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

export function mapInstance(raw: Record<string, unknown>): WorkflowInstance {
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
