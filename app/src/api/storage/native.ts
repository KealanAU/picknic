// Contracts for the native modules a Lynx host app exposes on `NativeModules`.
//
// None of this runs in Lynx Explorer or web preview — those have no custom host
// module, so the app falls back to localStorage (web) or in-memory (Explorer).
// Implement these in your iOS/Android host to get real persistence; the drivers
// pick them up automatically.

declare const NativeModules: Record<string, any> | undefined;

/** Safely look up a native module by name; null when absent (or off-device). */
export function nativeModule<T = any>(name: string): T | null {
  try {
    return typeof NativeModules !== 'undefined'
      ? ((NativeModules[name] ?? null) as T | null)
      : null;
  } catch {
    return null;
  }
}

/**
 * Key/value module — the light option, best for credentials/settings.
 * Back it with SharedPreferences + Android Keystore (Android) and
 * UserDefaults + Keychain (iOS). `getItem` is callback-style (Lynx's async
 * native-module convention); `setItem`/`removeItem` are fire-and-forget.
 *
 *   Kotlin:  fun getItem(key: String, callback: Callback)
 *   Swift:   func getItem(_ key: String, callback: @escaping (String?) -> Void)
 */
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

/**
 * SQLite module — the relational option, for offline caches (events, photos,
 * guests). Back it with android.database.sqlite (Android) and libsqlite3 / a
 * bundled SQLite (iOS). One statement per call; `params` bind `?` placeholders.
 *
 *   execute("SELECT * FROM events WHERE code = ?", ["SARAH-MAX"], onOk, onErr)
 */
export interface NativeSqliteModule {
  execute(
    sql: string,
    params: Array<string | number | null>,
    onSuccess: (result: SqliteResult) => void,
    onError: (message: string) => void,
  ): void;
}
