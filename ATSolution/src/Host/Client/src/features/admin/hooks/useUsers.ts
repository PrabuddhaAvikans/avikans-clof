import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  roleService,
  userService,
  type RoleFormData,
  type RoleGroupFormData,
  type RoleListFilters,
  type UserFormData,
  type UserListFilters,
} from "@/services";

export const userKeys = {
  all: ["users"] as const,
  lists: () => [...userKeys.all, "list"] as const,
  list: (filters: UserListFilters) => [...userKeys.lists(), filters] as const,
  details: () => [...userKeys.all, "detail"] as const,
  detail: (id: string) => [...userKeys.details(), id] as const,
  permissions: (id: string) => [...userKeys.all, "permissions", id] as const,
};

export const roleKeys = {
  all: ["roles"] as const,
  lists: () => [...roleKeys.all, "list"] as const,
  list: (filters: RoleListFilters) => [...roleKeys.lists(), filters] as const,
  details: () => [...roleKeys.all, "detail"] as const,
  detail: (id: string) => [...roleKeys.details(), id] as const,
};

export const roleGroupKeys = {
  all: ["roleGroups"] as const,
  lists: () => [...roleGroupKeys.all, "list"] as const,
  list: (filters: RoleListFilters) => [...roleGroupKeys.lists(), filters] as const,
  details: () => [...roleGroupKeys.all, "detail"] as const,
  detail: (id: string) => [...roleGroupKeys.details(), id] as const,
};

export function useUsers(filters: UserListFilters) {
  return useQuery({
    queryKey: userKeys.list(filters),
    queryFn: () => userService.list(filters),
  });
}

export function useUser(id: string) {
  return useQuery({
    queryKey: userKeys.detail(id),
    queryFn: () => userService.getById(id),
    enabled: Boolean(id),
  });
}

export function useUserPermissions(userId: string) {
  return useQuery({
    queryKey: userKeys.permissions(userId),
    queryFn: () => userService.getPermissionAssignment(userId),
    enabled: Boolean(userId),
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UserFormData) => userService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: userKeys.all });
    },
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<UserFormData> }) =>
      userService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: userKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}

export function useRoles(filters: RoleListFilters) {
  return useQuery({
    queryKey: roleKeys.list(filters),
    queryFn: () => roleService.listRoles(filters),
  });
}

export function useRole(id: string) {
  return useQuery({
    queryKey: roleKeys.detail(id),
    queryFn: () => roleService.getRoleById(id),
    enabled: Boolean(id),
  });
}

export function useCreateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: RoleFormData) => roleService.createRole(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all });
    },
  });
}

export function useUpdateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<RoleFormData> }) =>
      roleService.updateRole(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: roleKeys.lists() });
    },
  });
}

export function useRoleGroups(filters: RoleListFilters) {
  return useQuery({
    queryKey: roleGroupKeys.list(filters),
    queryFn: () => roleService.listRoleGroups(filters),
  });
}

export function useRoleGroup(id: string) {
  return useQuery({
    queryKey: roleGroupKeys.detail(id),
    queryFn: () => roleService.getRoleGroupById(id),
    enabled: Boolean(id),
  });
}

export function useCreateRoleGroup() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: RoleGroupFormData) => roleService.createRoleGroup(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: roleGroupKeys.all });
    },
  });
}

export function useUpdateRoleGroup() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<RoleGroupFormData> }) =>
      roleService.updateRoleGroup(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: roleGroupKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: roleGroupKeys.lists() });
    },
  });
}
