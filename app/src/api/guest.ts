import { storage } from './storage';

export interface GuestSession {
  eventId: string;
  code: string;
  guestId: string;
  name: string;
  token: string;
  expiresAt: number;
}

const KEY = 'picknic.guest.session';

let current: GuestSession | null = null;

export function getGuest(): GuestSession | null {
  return current;
}

export function guestValid(skewMs = 15_000): boolean {
  return !!current && Date.now() < current.expiresAt - skewMs;
}

export async function loadGuest(): Promise<GuestSession | null> {
  const raw = await storage.getItem(KEY);
  if (!raw) return null;
  try {
    current = JSON.parse(raw) as GuestSession;
  } catch {
    current = null;
    await storage.removeItem(KEY);
  }
  return current;
}

export async function saveGuest(session: GuestSession): Promise<void> {
  current = session;
  await storage.setItem(KEY, JSON.stringify(session));
}

export async function clearGuest(): Promise<void> {
  current = null;
  await storage.removeItem(KEY);
}
