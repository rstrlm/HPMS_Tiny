import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import { User } from "oidc-client-ts";
import { userManager, getUser, login as oidcLogin, logout as oidcLogout } from "../lib/oidc";

export type Role = "manager" | "cleaner" | "therapist" | "frontdesk" | "maintenance" | "accounting";

type AuthState = {
  token?: string;
  roles: Role[];
  displayName?: string;
  isAuthenticated: boolean;
  isLoading: boolean;
};

type AuthContextValue = AuthState & {
  login: () => void;
  logout: () => void;
  setRoles: (roles: Role[]) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const ROLES_STORAGE_KEY = "pms_roles";
const TOKEN_STORAGE_KEY = "pms_access_token";

const ALL_ROLES: Role[] = ["manager", "cleaner", "therapist", "frontdesk", "maintenance", "accounting"];

const parseRoles = (value: string | null): Role[] => {
  if (!value) return [];
  return value
    .split(",")
    .map((role) => role.trim())
    .filter(Boolean)
    .filter((role): role is Role => ALL_ROLES.includes(role as Role));
};

const extractRolesFromToken = (token?: string): Role[] => {
  if (!token) return [];
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return [];
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const decoded = atob(payload.padEnd(payload.length + (4 - (payload.length % 4)) % 4, "="));
    const parsed = JSON.parse(decoded) as Record<string, unknown>;
    const realmAccess = parsed["realm_access"] as { roles?: string[] } | undefined;
    const rawRoles = realmAccess?.roles ?? [];
    return rawRoles.filter((role): role is Role => ALL_ROLES.includes(role as Role));
  } catch {
    return [];
  }
};

const extractDisplayName = (user: User | null): string | undefined => {
  if (!user?.profile) return undefined;
  return user.profile.name ?? user.profile.preferred_username ?? undefined;
};

// Check if we're in dev mode (no Keycloak URL configured or explicitly disabled)
const isDevMode = () => {
  const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL;
  const devMode = import.meta.env.VITE_DEV_MODE;
  return devMode === "true" || (!keycloakUrl && import.meta.env.DEV);
};

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(!isDevMode());
  const [devRoles, setDevRoles] = useState<Role[]>(() => {
    return parseRoles(localStorage.getItem(ROLES_STORAGE_KEY));
  });

  // Initialize OIDC on mount (only in production mode)
  useEffect(() => {
    if (isDevMode()) {
      setIsLoading(false);
      return;
    }

    const initAuth = async () => {
      try {
        const currentUser = await getUser();
        if (currentUser && !currentUser.expired) {
          setUser(currentUser);
          localStorage.setItem(TOKEN_STORAGE_KEY, currentUser.access_token);
        }
      } catch (error) {
        console.error("Failed to initialize auth:", error);
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();

    // Listen for token updates
    const handleUserLoaded = (loadedUser: User) => {
      setUser(loadedUser);
      localStorage.setItem(TOKEN_STORAGE_KEY, loadedUser.access_token);
    };

    const handleUserUnloaded = () => {
      setUser(null);
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      localStorage.removeItem(ROLES_STORAGE_KEY);
    };

    userManager.events.addUserLoaded(handleUserLoaded);
    userManager.events.addUserUnloaded(handleUserUnloaded);

    return () => {
      userManager.events.removeUserLoaded(handleUserLoaded);
      userManager.events.removeUserUnloaded(handleUserUnloaded);
    };
  }, []);

  const login = () => {
    if (isDevMode()) {
      // In dev mode, just show the role switcher
      return;
    }
    oidcLogin();
  };

  const logout = () => {
    if (isDevMode()) {
      setDevRoles([]);
      localStorage.removeItem(ROLES_STORAGE_KEY);
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      return;
    }
    oidcLogout();
  };

  const setRoles = (nextRoles: Role[]) => {
    setDevRoles(nextRoles);
    if (nextRoles.length > 0) {
      localStorage.setItem(ROLES_STORAGE_KEY, nextRoles.join(","));
    } else {
      localStorage.removeItem(ROLES_STORAGE_KEY);
    }
  };

  // Compute derived state
  const token = user?.access_token ?? localStorage.getItem(TOKEN_STORAGE_KEY) ?? undefined;
  const roles = isDevMode()
    ? devRoles
    : user?.access_token
    ? extractRolesFromToken(user.access_token)
    : parseRoles(localStorage.getItem(ROLES_STORAGE_KEY));
  const displayName = extractDisplayName(user);
  const isAuthenticated = isDevMode() ? devRoles.length > 0 : !!user && !user.expired;

  const value = useMemo(
    () => ({
      token,
      roles,
      displayName,
      isAuthenticated,
      isLoading,
      login,
      logout,
      setRoles
    }),
    [token, roles, displayName, isAuthenticated, isLoading, devRoles]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
};

export const hasAnyRole = (roles: Role[], allowed: Role[]) => {
  if (allowed.length === 0) return true;
  return roles.some((role) => allowed.includes(role));
};

export const getAccessToken = () => localStorage.getItem(TOKEN_STORAGE_KEY) ?? undefined;

export const getDevRoles = (): Role[] => {
  const envValue = import.meta.env.VITE_DEV_ROLES as string | undefined;
  if (!envValue) return [];
  return parseRoles(envValue);
};

export const isDevModeEnabled = isDevMode;
