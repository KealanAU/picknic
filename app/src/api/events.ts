// Public event lookup + guest join against the .NET API. Joining is
// unauthenticated (no host bearer): a guest presents a room code and their
// name, plus — when they arrived via a scanned QR — the high-entropy join
// secret. The API mints a scoped guest JWT in return (see api/guest.ts).
import { request } from './http';

export interface PublicEvent {
  code: string;
  name: string;
  uploadOpen: boolean;
  revealed: boolean;
  uploadClosesAt: string;
  revealAt: string;
}

export interface JoinResult {
  guestId: string;
  name: string;
  /** Scoped guest JWT — the bearer for upload calls. */
  token: string;
  /** ISO; matches the event's upload close (token expiry). */
  expiresAt: string;
  eventId: string;
}

/** Look up an event by its room code. 404s when the code is unknown. */
export function getEvent(code: string): Promise<PublicEvent> {
  return request<PublicEvent>(`/api/events/${encodeURIComponent(code.trim().toUpperCase())}`, {
    auth: false,
  });
}

/**
 * Join an event. `joinSecret` is optional — the room code plus the open upload
 * window are a sufficient gate; a scanned QR supplies the secret as a bonus.
 */
export function joinEvent(
  code: string,
  name: string,
  joinSecret?: string,
): Promise<JoinResult> {
  return request<JoinResult>(`/api/events/${encodeURIComponent(code.trim().toUpperCase())}/join`, {
    auth: false,
    body: { name: name.trim(), joinSecret },
  });
}
