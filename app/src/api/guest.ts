// Holds the joined guest's capability token for one event, persisted so the
// roll survives an app restart until the upload window closes. This is the
// guest counterpart to api/tokens.ts (which holds the host's Identity tokens).
// Pure state — no network calls (see api/events.ts).
import { storage } from './storage';

export interface GuestSession {
  eventId: string;
  /** The room code they joined with. */
  code: string;
  guestId: string;
  name: string;
  /** Scoped guest JWT — sent as the bearer on upload calls. */
  token: string;
  /** Epoch ms when the token expires (mirrors the event's upload close). */
  expiresAt: number;
}

const KEY = 'picknic.guest.session';

let current: GuestSession | null = null;

export function getGuest(): GuestSession | null {
  return current;
}

/** True while the guest token is present and unexpired. */
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
