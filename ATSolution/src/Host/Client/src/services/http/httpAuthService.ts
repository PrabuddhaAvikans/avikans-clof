import type { AuthUser } from "@/app/store/authSlice";
import { apiRequest, writeAuthToken } from "@/services/apiClient";
import { isAuthPersisted } from "@/app/store/authStorage";
import type { AuthService, LoginCredentials } from "@/services/interfaces/authService";

interface LoginResponse {
  token: string;
  user: AuthUser;
}

export const httpAuthService: AuthService = {
  async login(credentials: LoginCredentials): Promise<AuthUser> {
    const result = await apiRequest<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify(credentials),
    });
    writeAuthToken(result.token, isAuthPersisted());
    const user = result.user;
    return {
      id: String(user.id),
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      displayName: user.displayName || `${user.firstName} ${user.lastName}`.trim(),
      role: user.role,
      permissions: user.permissions ?? [],
    };
  },
};
