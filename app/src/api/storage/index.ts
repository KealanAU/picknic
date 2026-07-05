// Persistent key/value storage. Picks the first available backend:
// native KV -> SQLite -> localStorage (web) -> memory (non-persistent fallback).
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

export function setStorageDriver(driver: StorageDriver): void {
  active = driver;
  ready = null;
}

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
