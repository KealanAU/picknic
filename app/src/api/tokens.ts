import { storage } from './storage';

export interface TokenSet {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

const KEY = 'picknic.auth.tokens';

let current: TokenSet | null = null;
let currentRemember = false;

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
    currentRemember = true;
  } catch {
    current = null;
    currentRemember = false;
  }
  return current;
}

export async function saveTokens(tokens: TokenSet, remember = currentRemember): Promise<void> {
  current = tokens;
  currentRemember = remember;
  if (remember) await storage.setItem(KEY, JSON.stringify(tokens));
  else await storage.removeItem(KEY);
}

export async function clearTokens(): Promise<void> {
  current = null;
  currentRemember = false;
  await storage.removeItem(KEY);
}
