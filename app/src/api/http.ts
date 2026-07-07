import { apiUrl } from './config';
import { getTokens, isExpired } from './tokens';

// Lynx injects `fetch` into the bundle scope (tt.fetch). Under the rspeedy web
// preview that binding has no working network bridge, while the real fetch on
// globalThis does; prefer it and fall back to the injected one for native.
export const platformFetch: typeof fetch = globalThis.fetch
  ? globalThis.fetch.bind(globalThis)
  : fetch;

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly problem?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function isApiError(e: unknown): e is ApiError {
  return !!e
    && typeof e === 'object'
    && (e as { name?: unknown }).name === 'ApiError'
    && typeof (e as { status?: unknown }).status === 'number'
    && typeof (e as { message?: unknown }).message === 'string';
}

// Registered by auth.ts to break the http<->auth import cycle.
let refreshHandler: (() => Promise<boolean>) | null = null;
export function setRefreshHandler(fn: () => Promise<boolean>): void {
  refreshHandler = fn;
}

export interface RequestOptions {
  method?: string;
  body?: unknown;
  auth?: boolean;
  token?: string;
  query?: Record<string, string | number | boolean | undefined>;
  headers?: Record<string, string>;
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = apiUrl(path);
  if (!query) return url;
  const qs = Object.entries(query)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
    .join('&');
  return qs ? `${url}?${qs}` : url;
}

async function parse(res: Response): Promise<unknown> {
  const text = await res.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

// Extracts a human message from an RFC-7807 problem+json payload
// ({ title, detail?, status, errors? }), legacy { error } bodies, or plain text.
export function problemMessage(payload: unknown): string | null {
  if (payload && typeof payload === 'object') {
    const p = payload as Record<string, unknown>;
    if (p.errors && typeof p.errors === 'object') {
      for (const messages of Object.values(p.errors as Record<string, unknown>)) {
        if (Array.isArray(messages) && typeof messages[0] === 'string' && messages[0]) {
          return messages[0];
        }
      }
    }
    if (typeof p.detail === 'string' && p.detail) return p.detail;
    if (typeof p.title === 'string' && p.title) return p.title;
    if (typeof p.error === 'string' && p.error) return p.error;
  }
  if (typeof payload === 'string' && payload.trim()) return payload.trim();
  return null;
}

function messageFor(status: number, payload: unknown): string {
  return problemMessage(payload) ?? `Something went wrong (${status}).`;
}

export async function request<T = unknown>(
  path: string,
  opts: RequestOptions = {},
): Promise<T> {
  const useAuth = opts.auth !== false && !opts.token;

  if (useAuth && getTokens() && isExpired() && refreshHandler) {
    await refreshHandler();
  }

  const send = async (): Promise<Response> => {
    const headers: Record<string, string> = { Accept: 'application/json', ...opts.headers };
    if (opts.body !== undefined) headers['Content-Type'] = 'application/json';

    const token = opts.token ?? (useAuth ? getTokens()?.accessToken : undefined);
    if (token) headers.Authorization = `Bearer ${token}`;

    return platformFetch(buildUrl(path, opts.query), {
      method: opts.method ?? (opts.body !== undefined ? 'POST' : 'GET'),
      headers,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  };

  let res = await send();

  if (res.status === 401 && useAuth && refreshHandler && getTokens()) {
    if (await refreshHandler()) res = await send();
  }

  const payload = await parse(res);
  if (!res.ok) throw new ApiError(res.status, messageFor(res.status, payload), payload);
  return payload as T;
}
