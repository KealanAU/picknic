// Reactive wrapper over the guest-join API + session store. A module-level
// singleton so every screen shares one guest state, mirroring useAuth for
// hosts. Guests don't have accounts — "joined" is their authenticated state.
import { computed, reactive } from 'vue';
import { getEvent, joinEvent, type PublicEvent } from '../api/events';
import { clearGuest, guestValid, loadGuest, saveGuest, type GuestSession } from '../api/guest';
import { ApiError } from '../api/http';

type Status = 'idle' | 'loading' | 'joined' | 'none';

const state = reactive({
  session: null as GuestSession | null,
  status: 'idle' as Status,
  error: null as string | null,
});

function friendly(e: unknown): string {
  if (e instanceof ApiError) {
    if (e.status === 404) return "That event code doesn't exist.";
    if (e.status === 401) return 'That join link is invalid — ask the host for the QR.';
    if (e.status === 403) return 'Uploads are closed for this event.';
    if (e.status === 429) return 'Too many attempts. Give it a minute.';
    return e.message;
  }
  return 'Something went wrong. Try again.';
}

export function useGuest() {
  return {
    session: computed(() => state.session),
    status: computed(() => state.status),
    error: computed(() => state.error),
    isJoined: computed(() => state.status === 'joined'),
    /** Settled once we know whether a persisted session exists. */
    isReady: computed(() => state.status === 'joined' || state.status === 'none'),

    /** Restore a persisted guest session on app start. */
    async initialize(): Promise<void> {
      state.status = 'loading';
      const session = await loadGuest();
      if (session && guestValid()) {
        state.session = session;
        state.status = 'joined';
      } else {
        if (session) await clearGuest(); // expired — don't leave it lingering
        state.status = 'none';
      }
    },

    /** Validate a room code before asking for a name. Returns the event. */
    async lookup(code: string): Promise<PublicEvent> {
      state.error = null;
      try {
        return await getEvent(code);
      } catch (e) {
        state.error = friendly(e);
        throw e;
      }
    },

    /** Join with a code + name (+ optional QR secret). */
    async join(code: string, name: string, secret?: string): Promise<void> {
      state.status = 'loading';
      state.error = null;
      try {
        const res = await joinEvent(code, name, secret);
        const session: GuestSession = {
          eventId: res.eventId,
          code: code.trim().toUpperCase(),
          guestId: res.guestId,
          name: res.name,
          token: res.token,
          expiresAt: new Date(res.expiresAt).getTime(),
        };
        await saveGuest(session);
        state.session = session;
        state.status = 'joined';
      } catch (e) {
        state.error = friendly(e);
        state.status = state.session ? 'joined' : 'none';
        throw e;
      }
    },

    async leave(): Promise<void> {
      await clearGuest();
      state.session = null;
      state.error = null;
      state.status = 'none';
    },
  };
}
