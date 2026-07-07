// Reactive wrapper over api/auth.ts, a module-level singleton so every screen
// shares one auth state.
import { computed, reactive } from 'vue';
import * as auth from '../api/auth';
import type { AccountInfo } from '../api/auth';
import { isApiError, problemMessage } from '../api/http';
import { sanitizeEmail } from '../api/sanitize';

type Status = 'idle' | 'loading' | 'authenticated' | 'unauthenticated';

const state = reactive({
  user: null as AccountInfo | null,
  status: 'idle' as Status,
  busy: false,
  error: null as string | null,
});

function fail(e: unknown): never {
  state.error = authMessage(e);
  state.status = state.user ? 'authenticated' : 'unauthenticated';
  state.busy = false;
  throw e;
}

function authMessage(e: unknown): string {
  if (!isApiError(e)) return 'Something went wrong';
  const server = problemMessage(e.problem);
  if (server) return server;
  if (e.status === 400 || e.status === 401) return 'Invalid email or password';
  return e.message || 'Something went wrong';
}

function accountAlreadyExists(e: unknown): boolean {
  if (!isApiError(e) || e.status !== 400) return false;
  const message = e.message.toLowerCase();
  return message.includes('already') || message.includes('taken');
}

async function refreshUser(): Promise<void> {
  state.user = await auth.getInfo();
  state.status = 'authenticated';
}

export function useAuth() {
  return {
    user: computed(() => state.user),
    status: computed(() => state.status),
    error: computed(() => state.error),
    isAuthenticated: computed(() => state.status === 'authenticated'),
    isBusy: computed(() => state.busy || state.status === 'loading'),

    async initialize(): Promise<void> {
      state.status = 'loading';
      state.busy = true;
      try {
        if (await auth.restoreSession()) await refreshUser();
        else state.status = 'unauthenticated';
      } catch {
        await auth.logout();
        state.status = 'unauthenticated';
      } finally {
        state.busy = false;
      }
    },

    async register(email: string, password: string, remember = true): Promise<void> {
      state.busy = true;
      state.error = null;
      try {
        const normalizedEmail = sanitizeEmail(email);
        try {
          await auth.register(normalizedEmail, password);
        } catch (e) {
          if (!accountAlreadyExists(e)) throw e;
        }
        await auth.login(normalizedEmail, password, remember);
        await refreshUser();
        state.busy = false;
      } catch (e) {
        fail(e);
      }
    },

    async login(email: string, password: string, remember = true): Promise<void> {
      state.busy = true;
      state.error = null;
      try {
        await auth.login(sanitizeEmail(email), password, remember);
        await refreshUser();
        state.busy = false;
      } catch (e) {
        fail(e);
      }
    },

    async logout(): Promise<void> {
      await auth.logout();
      state.user = null;
      state.error = null;
      state.busy = false;
      state.status = 'unauthenticated';
    },
  };
}
