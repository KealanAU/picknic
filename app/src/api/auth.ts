// Host account auth against the API's ASP.NET Core Identity endpoints
// (mapped at /api/auth via MapIdentityApi). These are the accounts that create
// and manage events — distinct from event guests, who use a separate JWT.
import { request, setRefreshHandler, ApiError } from './http';
import {
  clearTokens,
  getTokens,
  loadTokens,
  saveTokens,
  type TokenSet,
} from './tokens';

// Shape returned by Identity's /login and /refresh (AccessTokenResponse).
interface AccessTokenResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface AccountInfo {
  email: string;
  isEmailConfirmed: boolean;
}

function store(res: AccessTokenResponse): Promise<void> {
  const tokens: TokenSet = {
    accessToken: res.accessToken,
    refreshToken: res.refreshToken,
    expiresAt: Date.now() + res.expiresIn * 1000,
  };
  return saveTokens(tokens);
}

/** Create a host account. Does not log in — call `login` afterward. */
export function register(email: string, password: string): Promise<void> {
  return request('/api/auth/register', { auth: false, body: { email: email.trim(), password } });
}

/** Exchange credentials for tokens and persist them. */
export async function login(email: string, password: string): Promise<void> {
  const res = await request<AccessTokenResponse>('/api/auth/login', {
    auth: false,
    body: { email: email.trim(), password },
  });
  await store(res);
}

/**
 * Swap the stored refresh token for a new token pair. Registered as the http
 * layer's refresh handler, so callers rarely invoke it directly. Returns false
 * (and clears tokens) when the refresh token is gone or rejected.
 */
export async function refreshTokens(): Promise<boolean> {
  const tokens = getTokens();
  if (!tokens?.refreshToken) return false;
  try {
    const res = await request<AccessTokenResponse>('/api/auth/refresh', {
      auth: false,
      body: { refreshToken: tokens.refreshToken },
    });
    await store(res);
    return true;
  } catch (e) {
    if (e instanceof ApiError && e.status === 401) await clearTokens();
    return false;
  }
}

/** Current account. Throws ApiError(401) if not logged in / token invalid. */
export function getInfo(): Promise<AccountInfo> {
  return request<AccountInfo>('/api/auth/manage/info');
}

/** Change email and/or password. Leave a field undefined to keep it. */
export function updateAccount(changes: {
  newEmail?: string;
  newPassword?: string;
  oldPassword?: string;
}): Promise<AccountInfo> {
  return request<AccountInfo>('/api/auth/manage/info', { body: changes });
}

export function forgotPassword(email: string): Promise<void> {
  return request('/api/auth/forgotPassword', { auth: false, body: { email: email.trim() } });
}

export function resetPassword(
  email: string,
  resetCode: string,
  newPassword: string,
): Promise<void> {
  return request('/api/auth/resetPassword', {
    auth: false,
    body: { email: email.trim(), resetCode, newPassword },
  });
}

/** Forget tokens locally. Identity bearer tokens are stateless — nothing to revoke server-side. */
export function logout(): Promise<void> {
  return clearTokens();
}

/** Load persisted tokens on startup. Returns true if a session was restored. */
export async function restoreSession(): Promise<boolean> {
  const tokens = await loadTokens();
  return !!tokens?.accessToken;
}

// Let the http layer refresh transparently on 401 / near-expiry.
setRefreshHandler(refreshTokens);
