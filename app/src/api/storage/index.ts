// Persistent key/value storage with a pluggable backend.
//
// Priority: native KV (device) → SQLite (device, if that's the only module) →
// localStorage (web preview) → memory (Explorer fallback, not persistent).
// Swap explicitly with setStorageDriver() — e.g. force SQLite once your host
// implements NativeSqliteModule.
import type { KeyValueStore, StorageDriver } from './types';
import { memoryDriver, nativeKvDriver, sqliteKvDriver, webDriver } from './drivers';

export type { KeyValueStore, StorageDriver } from './types';
export type {
  NativeKVModule,
  NativeSqliteModule,
  SqliteResult,
  SqliteRow,
} from './native';
export { memoryDriver, nativeKvDriver, sqliteKvDriver, webDriver } from './drivers';

function selectDriver(): StorageDriver {
  const candidates = [nativeKvDriver(), sqliteKvDriver(), webDriver(), memoryDriver()];
  return candidates.find((d) => d.isAvailable()) ?? memoryDriver();
}

let active: StorageDriver = selectDriver();
let ready: Promise<void> | null = null;

function ensureReady(): Promise<void> {
  ready ??= active.init?.() ?? Promise.resolve();
  return ready;
}

/** Force a specific backend. Its init() re-runs on next use. */
export function setStorageDriver(driver: StorageDriver): void {
  active = driver;
  ready = null;
}

/** Name of the backend in use — log it at startup to see the active fallback. */
export function activeStorageName(): string {
  return active.name;
}

export const storage: KeyValueStore = {
  async getItem(key) {
    await ensureReady();
    return active.getItem(key);
  },
  async setItem(key, value) {
    await ensureReady();
    return active.setItem(key, value);
  },
  async removeItem(key) {
    await ensureReady();
    return active.removeItem(key);
  },
};
