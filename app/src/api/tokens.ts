import { storage } from './storage';

export interface TokenSet {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

const KEY = 'picknic.auth.tokens';

let current: TokenSet | null = null;

export function getTokens(): TokenSet | null {
  return current;
}

export function isExpired(skewMs = 15_000): boolean {
  return !current || Date.now() >= current.expiresAt - skewMs;
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
