// Contracts for the native modules a Lynx host app exposes on `NativeModules`.
// Absent in Lynx Explorer / web preview (they fall back to localStorage/memory).
// See native/README.md for the iOS/Android host implementations.

declare const NativeModules: Record<string, any> | undefined;

// Returns null when the module is absent (or running off-device).
export function nativeModule<T = any>(name: string): T | null {
  try {
    return typeof NativeModules !== 'undefined'
      ? ((NativeModules[name] ?? null) as T | null)
      : null;
  } catch {
    return null;
  }
}

// getItem is callback-style (Lynx async convention); the setters are fire-and-forget.
export interface NativeKVModule {
  getItem(key: string, callback: (value: string | null) => void): void;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

export interface SqliteRow {
  [column: string]: string | number | null;
}

export interface SqliteResult {
  rows: SqliteRow[];
  rowsAffected: number;
  insertId?: number;
}

// One statement per call; params bind the `?` placeholders.
export interface NativeSqliteModule {
  execute(
    sql: string,
    params: Array<string | number | null>,
    onSuccess: (result: SqliteResult) => void,
    onError: (message: string) => void,
  ): void;
}
