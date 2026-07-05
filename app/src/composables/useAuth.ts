// Reactive wrapper over api/auth.ts, a module-level singleton so every screen
// shares one auth state.
import { computed, reactive } from 'vue';
import * as auth from '../api/auth';
import type { AccountInfo } from '../api/auth';
import { ApiError } from '../api/http';

type Status = 'idle' | 'loading' | 'authenticated' | 'unauthenticated';

const state = reactive({
  user: null as AccountInfo | null,
  status: 'idle' as Status,
  error: null as string | null,
});

function fail(e: unknown): never {
  state.error = e instanceof ApiError ? e.message : 'Something went wrong';
  state.status = state.user ? 'authenticated' : 'unauthenticated';
  throw e;
}

function accountAlreadyExists(e: unknown): boolean {
  if (!(e instanceof ApiError) || e.status !== 400) return false;
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
    isBusy: computed(() => state.status === 'loading'),

    async initialize(): Promise<void> {
      state.status = 'loading';
      try {
        if (await auth.restoreSession()) await refreshUser();
        else state.status = 'unauthenticated';
      } catch {
        await auth.logout();
        state.status = 'unauthenticated';
      }
    },

    async register(email: string, password: string): Promise<void> {
      state.status = 'loading';
      state.error = null;
      try {
        const normalizedEmail = email.trim();
        try {
          await auth.register(normalizedEmail, password);
        } catch (e) {
          if (!accountAlreadyExists(e)) throw e;
        }
        await auth.login(normalizedEmail, password);
        await refreshUser();
      } catch (e) {
        fail(e);
      }
    },

    async login(email: string, password: string): Promise<void> {
      state.status = 'loading';
      state.error = null;
      try {
        await auth.login(email.trim(), password);
        await refreshUser();
      } catch (e) {
        fail(e);
      }
    },

    async logout(): Promise<void> {
      await auth.logout();
      state.user = null;
      state.error = null;
      state.status = 'unauthenticated';
    },
  };
}
