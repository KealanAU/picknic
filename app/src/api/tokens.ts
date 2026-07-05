// Holds the host account's bearer + refresh tokens, persisted so a login
// survives an app restart. Pure state — no network calls (see auth.ts).
import { storage } from './storage';

export interface TokenSet {
  accessToken: string;
  refreshToken: string;
  /** Epoch ms when the access token expires. */
  expiresAt: number;
}

const KEY = 'picknic.auth.tokens';

let current: TokenSet | null = null;

export function getTokens(): TokenSet | null {
  return current;
}

/** True when the access token is missing or within `skewMs` of expiring. */
export function isExpired(skewMs = 15_000): boolean {
  if (!current) return true;
  return Date.now() >= current.expiresAt - skewMs;
}

export async function loadTokens(): Promise<TokenSet | null> {
  const raw = await storage.getItem(KEY);
  if (!raw) return null;
  try {
    current = JSON.parse(raw) as TokenSet;
  } catch {
    current = null;
  }
  return current;
}

export async function saveTokens(tokens: TokenSet): Promise<void> {
  current = tokens;
  await storage.setItem(KEY, JSON.stringify(tokens));
}

export async function clearTokens(): Promise<void> {
  current = null;
  await storage.removeItem(KEY);
}
