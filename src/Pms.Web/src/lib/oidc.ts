import { UserManager, WebStorageStateStore, User } from "oidc-client-ts";

const keycloakConfig = {
  authority: import.meta.env.VITE_KEYCLOAK_URL ?? "http://localhost:8080/realms/pms",
  client_id: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? "pms-web",
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: "code",
  scope: "openid profile email",
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage })
};

export const userManager = new UserManager(keycloakConfig);

export const login = () => userManager.signinRedirect();

export const logout = () => userManager.signoutRedirect();

export const handleCallback = async (): Promise<User> => {
  return userManager.signinRedirectCallback();
};

export const getUser = async (): Promise<User | null> => {
  return userManager.getUser();
};

export const silentRenew = async (): Promise<User | null> => {
  try {
    return await userManager.signinSilent();
  } catch {
    return null;
  }
};

// Extract roles from Keycloak token
export const extractRoles = (user: User | null): string[] => {
  if (!user?.access_token) return [];

  try {
    const payload = user.access_token.split(".")[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
    const realmAccess = decoded.realm_access as { roles?: string[] } | undefined;
    return realmAccess?.roles ?? [];
  } catch {
    return [];
  }
};
