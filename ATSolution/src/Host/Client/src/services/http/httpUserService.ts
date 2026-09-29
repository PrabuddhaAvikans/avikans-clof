import type { PaginatedResponse } from "@/types/common";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  RoleFormData,
  RoleGroupFormData,
  RoleListFilters,
  RoleService,
  UserFormData,
  UserListFilters,
  UserService,
} from "@/services/interfaces/userService";
import type {
  PermissionAssignment,
  Role,
  RoleGroup,
  User,
} from "@/types/user";

function mapUser(raw: Record<string, unknown>): User {
  return {
    id: String(raw.id),
    email: String(raw.email),
    firstName: String(raw.firstName),
    lastName: String(raw.lastName),
    displayName: String(raw.displayName ?? `${raw.firstName} ${raw.lastName}`),
    phone: raw.phone as string | undefined,
    avatarUrl: raw.avatarUrl as string | undefined,
    roleId: String(raw.roleId),
    roleName: String(raw.roleName ?? ""),
    roleGroupIds: ((raw.roleGroupIds as string[]) ?? []).map(String),
    roleGroupNames: ((raw.roleGroupNames as string[]) ?? []).map(String),
    department: raw.department as string | undefined,
    jobTitle: raw.jobTitle as string | undefined,
    status: (raw.status as User["status"]) ?? "active",
    lastLoginAt: raw.lastLoginAt as string | undefined,
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.modifiedOnUtc ?? new Date().toISOString()),
  };
}

function mapRole(raw: Record<string, unknown>): Role {
  return {
    id: String(raw.id),
    name: String(raw.name),
    description: raw.description as string | undefined,
    permissions: (raw.permissions as Role["permissions"]) ?? [],
    isSystem: Boolean(raw.isSystem),
    userCount: Number(raw.userCount ?? 0),
    status: (raw.status as Role["status"]) ?? "active",
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.modifiedOnUtc ?? new Date().toISOString()),
  };
}

function mapRoleGroup(raw: Record<string, unknown>): RoleGroup {
  return {
    id: String(raw.id),
    name: String(raw.name),
    description: raw.description as string | undefined,
    roleIds: ((raw.roleIds as string[]) ?? []).map(String),
    roleNames: ((raw.roleNames as string[]) ?? []).map(String),
    userCount: Number(raw.userCount ?? 0),
    status: (raw.status as RoleGroup["status"]) ?? "active",
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.modifiedOnUtc ?? new Date().toISOString()),
  };
}

function mapPage<T>(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
  mapItem: (item: Record<string, unknown>) => T,
): PaginatedResponse<T> {
  return {
    items: (raw.items ?? []).map((item) => mapItem(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpUserService: UserService = {
  async list(filters: UserListFilters) {
    const raw = await apiRequest<Parameters<typeof mapPage>[0]>(
      `/api/users${buildQuery(filters as unknown as Record<string, unknown>)}`,
    );
    return mapPage(raw, mapUser);
  },
  async getById(id: string) {
    const raw = await apiRequest<Record<string, unknown>>(`/api/users/${id}`);
    return mapUser(raw);
  },
  async create(data: UserFormData) {
    const raw = await apiRequest<Record<string, unknown>>("/api/users", {
      method: "POST",
      body: JSON.stringify(data),
    });
    return mapUser(raw);
  },
  async update(id: string, data: Partial<UserFormData>) {
    const raw = await apiRequest<Record<string, unknown>>(`/api/users/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
    return mapUser(raw);
  },
  async delete(id: string) {
    await apiRequest<void>(`/api/users/${id}`, { method: "DELETE" });
  },
  async getPermissionAssignment(userId: string) {
    return apiRequest<PermissionAssignment>(`/api/users/${userId}/permission-assignment`);
  },
};

export const httpRoleService: RoleService = {
  async listRoles(filters: RoleListFilters) {
    const raw = await apiRequest<Parameters<typeof mapPage>[0]>(
      `/api/roles${buildQuery(filters as unknown as Record<string, unknown>)}`,
    );
    return mapPage(raw, mapRole);
  },
  async getRoleById(id: string) {
    return mapRole(await apiRequest<Record<string, unknown>>(`/api/roles/${id}`));
  },
  async createRole(data: RoleFormData) {
    return mapRole(
      await apiRequest<Record<string, unknown>>("/api/roles", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    );
  },
  async updateRole(id: string, data: Partial<RoleFormData>) {
    return mapRole(
      await apiRequest<Record<string, unknown>>(`/api/roles/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    );
  },
  async deleteRole(id: string) {
    await apiRequest<void>(`/api/roles/${id}`, { method: "DELETE" });
  },
  async listRoleGroups(filters: RoleListFilters) {
    const raw = await apiRequest<Parameters<typeof mapPage>[0]>(
      `/api/role-groups${buildQuery(filters as unknown as Record<string, unknown>)}`,
    );
    return mapPage(raw, mapRoleGroup);
  },
  async getRoleGroupById(id: string) {
    return mapRoleGroup(await apiRequest<Record<string, unknown>>(`/api/role-groups/${id}`));
  },
  async createRoleGroup(data: RoleGroupFormData) {
    return mapRoleGroup(
      await apiRequest<Record<string, unknown>>("/api/role-groups", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    );
  },
  async updateRoleGroup(id: string, data: Partial<RoleGroupFormData>) {
    return mapRoleGroup(
      await apiRequest<Record<string, unknown>>(`/api/role-groups/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    );
  },
  async deleteRoleGroup(id: string) {
    await apiRequest<void>(`/api/role-groups/${id}`, { method: "DELETE" });
  },
};
