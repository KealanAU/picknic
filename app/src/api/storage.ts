type StorageLike = {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
};

const memory = new Map<string, string>();

const memoryStorage: StorageLike = {
  getItem: key => memory.get(key) ?? null,
  setItem: (key, value) => {
    memory.set(key, value);
  },
  removeItem: key => {
    memory.delete(key);
  },
};

function browserStorage(): StorageLike | null {
  try {
    if (typeof globalThis.localStorage === 'undefined') return null;
    const testKey = '__picknic_storage_test__';
    globalThis.localStorage.setItem(testKey, '1');
    globalThis.localStorage.removeItem(testKey);
    return globalThis.localStorage;
  } catch {
    return null;
  }
}

const selected = browserStorage();

export const storage: StorageLike = selected ?? memoryStorage;

export function activeStorageName(): 'localStorage' | 'memory' {
  return selected ? 'localStorage' : 'memory';
}
