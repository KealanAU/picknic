import type { StorageDriver } from './types';
import { nativeModule, type NativeKVModule } from './native';

// Not persistent — lost on reload (e.g. Lynx Explorer).
export function memoryDriver(): StorageDriver {
  const mem = new Map<string, string>();
  return {
    name: 'memory',
    isAvailable: () => true,
    getItem: async (k) => mem.get(k) ?? null,
    setItem: async (k, v) => {
      mem.set(k, v);
    },
    removeItem: async (k) => {
      mem.delete(k);
    },
  };
}

export function webDriver(): StorageDriver {
  let ok = false;
  try {
    ok = typeof localStorage !== 'undefined';
  } catch {
    ok = false; // sandboxed contexts can throw on access
  }
  return {
    name: 'localStorage',
    isAvailable: () => ok,
    getItem: async (k) => localStorage.getItem(k),
    setItem: async (k, v) => localStorage.setItem(k, v),
    removeItem: async (k) => localStorage.removeItem(k),
  };
}

// Adapts our NativeKVModule or Lynx's NativeLocalStorageModule, and tolerates
// hosts whose getter returns synchronously instead of via callback.
export function nativeKvDriver(): StorageDriver {
  const mod: any =
    nativeModule<NativeKVModule>('NativeKVModule') ??
    nativeModule<any>('NativeLocalStorageModule');
  const get = mod?.getItem ?? mod?.getStorageItem;
  const set = mod?.setItem ?? mod?.setStorageItem;
  const remove = mod?.removeItem ?? mod?.removeStorageItem;

  return {
    name: 'native-kv',
    isAvailable: () => !!(mod && get && set),
    getItem: (key) =>
      new Promise((resolve) => {
        // Absent keys may arrive as null, undefined, NSNull-ish objects, or ''
        // (the blanking-removal convention below) depending on the host bridge.
        const norm = (v: unknown) => (typeof v === 'string' && v !== '' ? v : null);
        try {
          // Some hosts return the value synchronously; others resolve via the callback.
          const syncResult = get.call(mod, key, (v: unknown) => resolve(norm(v)));
          if (syncResult !== undefined) resolve(norm(syncResult));
        } catch {
          resolve(null);
        }
      }),
    setItem: async (key, value) => {
      set.call(mod, key, value);
    },
    removeItem: async (key) => {
      if (remove) remove.call(mod, key);
      else set.call(mod, key, ''); // blanking ≈ removal when no remove method
    },
  };
}
