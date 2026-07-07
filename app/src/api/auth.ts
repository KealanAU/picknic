// Host account auth against the API's Identity endpoints (MapIdentityApi at
// /api/auth). Distinct from event guests, who use a separate JWT.
import { request, setRefreshHandler, isApiError } from './http';
import {
  clearTokens,
  getTokens,
  loadTokens,
  saveTokens,
  type TokenSet,
} from './tokens';
import { sanitizeEmail } from './sanitize';

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

function store(res: AccessTokenResponse, remember?: boolean): Promise<void> {
  const tokens: TokenSet = {
    accessToken: res.accessToken,
    refreshToken: res.refreshToken,
    expiresAt: Date.now() + res.expiresIn * 1000,
  };
  return saveTokens(tokens, remember);
}

export function register(email: string, password: string): Promise<void> {
  return request('/api/auth/register', { auth: false, body: { email: sanitizeEmail(email), password } });
}

export async function login(email: string, password: string, remember = true): Promise<void> {
  const res = await request<AccessTokenResponse>('/api/auth/login', {
    auth: false,
    body: { email: sanitizeEmail(email), password },
  });
  await store(res, remember);
}

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
    if (isApiError(e) && e.status === 401) await clearTokens();
    return false;
  }
}

export function getInfo(): Promise<AccountInfo> {
  return request<AccountInfo>('/api/auth/manage/info');
}

export function updateAccount(changes: {
  newEmail?: string;
  newPassword?: string;
  oldPassword?: string;
}): Promise<AccountInfo> {
  return request<AccountInfo>('/api/auth/manage/info', {
    body: {
      ...changes,
      newEmail: changes.newEmail === undefined ? undefined : sanitizeEmail(changes.newEmail),
    },
  });
}

export function forgotPassword(email: string): Promise<void> {
  return request('/api/auth/forgotPassword', { auth: false, body: { email: sanitizeEmail(email) } });
}

export function resetPassword(
  email: string,
  resetCode: string,
  newPassword: string,
): Promise<void> {
  return request('/api/auth/resetPassword', {
    auth: false,
    body: { email: sanitizeEmail(email), resetCode, newPassword },
  });
}

export function logout(): Promise<void> {
  return clearTokens();
}

export async function restoreSession(): Promise<boolean> {
  const tokens = await loadTokens();
  return !!tokens?.accessToken;
}

setRefreshHandler(refreshTokens);
