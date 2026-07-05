// Reactive wrapper over the guest-join API + session store, a module-level
// singleton mirroring useAuth. Guests have no account — "joined" is their auth state.
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
    isReady: computed(() => state.status === 'joined' || state.status === 'none'),

    async initialize(): Promise<void> {
      state.status = 'loading';
      const session = await loadGuest();
      if (session && guestValid()) {
        state.session = session;
        state.status = 'joined';
      } else {
        if (session) await clearGuest();
        state.status = 'none';
      }
    },

    async lookup(code: string): Promise<PublicEvent> {
      state.error = null;
      try {
        return await getEvent(code);
      } catch (e) {
        state.error = friendly(e);
        throw e;
      }
    },

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
