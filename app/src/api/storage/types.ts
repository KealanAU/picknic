export interface KeyValueStore {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

export interface StorageDriver extends KeyValueStore {
  readonly name: string;
  isAvailable(): boolean;
  init?(): Promise<void>;
}
