// Persistent key/value storage. Picks the first available backend:
// native KV -> localStorage (web) -> memory (non-persistent fallback).
import type { KeyValueStore, StorageDriver } from './types';
import { memoryDriver, nativeKvDriver, webDriver } from './drivers';

export type { KeyValueStore, StorageDriver } from './types';

const active: StorageDriver =
  [nativeKvDriver(), webDriver(), memoryDriver()].find((d) => d.isAvailable()) ?? memoryDriver();

export function activeStorageName(): string {
  return active.name;
}

export const storage: KeyValueStore = active;
