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
  token: string;
  expiresAt: string;
  eventId: string;
}

export function getEvent(code: string): Promise<PublicEvent> {
  return request<PublicEvent>(`/api/events/${encodeURIComponent(code.trim().toUpperCase())}`, {
    auth: false,
  });
}

// joinSecret is optional: a scanned QR supplies it, but the room code + open
// upload window are a sufficient gate on their own.
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
