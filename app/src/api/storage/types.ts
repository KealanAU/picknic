// Storage abstraction shared by all backends.

/** Async key/value store the app persists tokens/settings through. */
export interface KeyValueStore {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

/** A named backend that reports whether it can run in the current runtime. */
export interface StorageDriver extends KeyValueStore {
  /** For logs/diagnostics, e.g. "sqlite:kv". */
  readonly name: string;
  /** True if this backend is usable right now (module present, API exists, …). */
  isAvailable(): boolean;
  /** One-time setup (e.g. CREATE TABLE). Run once before first use. */
  init?(): Promise<void>;
}
