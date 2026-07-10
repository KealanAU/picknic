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

// DEV-only test party: code 00000 resolves and joins locally, so the guest flow
// is walkable on a phone that can't reach the local API. Prod builds drop these
// branches with import.meta.env.DEV.
const TEST_CODE = '00000';

function isTestCode(code: string): boolean {
  return !!import.meta.env.DEV && code.trim().toUpperCase() === TEST_CODE;
}

function testEvent(): PublicEvent {
  const now = Date.now();
  return {
    code: TEST_CODE,
    name: 'Test party',
    uploadOpen: true,
    revealed: false,
    uploadClosesAt: new Date(now + 4 * 3_600_000).toISOString(),
    revealAt: new Date(now + 5 * 3_600_000).toISOString(),
  };
}

export function getEvent(code: string): Promise<PublicEvent> {
  if (isTestCode(code)) return Promise.resolve(testEvent());
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
  if (isTestCode(code)) {
    return Promise.resolve({
      guestId: 'test-guest',
      name: name.trim(),
      token: 'test-token',
      expiresAt: new Date(Date.now() + 12 * 3_600_000).toISOString(),
      eventId: 'test-event',
    });
  }
  return request<JoinResult>(`/api/events/${encodeURIComponent(code.trim().toUpperCase())}/join`, {
    auth: false,
    body: { name: name.trim(), joinSecret },
  });
}
