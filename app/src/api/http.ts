import { apiUrl } from './config';
import { getTokens, isExpired } from './tokens';

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

function messageFor(status: number, payload: unknown): string {
  if (payload && typeof payload === 'object') {
    const p = payload as Record<string, unknown>;
    if (typeof p.detail === 'string') return p.detail;
    if (typeof p.title === 'string') return p.title;
    if (p.errors && typeof p.errors === 'object') {
      const first = Object.values(p.errors as Record<string, unknown>)[0];
      if (Array.isArray(first) && typeof first[0] === 'string') return first[0];
    }
  }
  if (typeof payload === 'string' && payload) return payload;
  return `Request failed (${status})`;
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

    return fetch(buildUrl(path, opts.query), {
      method: opts.method ?? (opts.body !== undefined ? 'POST' : 'GET'),
      headers,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  };

  let res = await send();

  // Token rejected mid-flight: refresh once and retry.
  if (res.status === 401 && useAuth && refreshHandler && getTokens()) {
    if (await refreshHandler()) res = await send();
  }

  const payload = await parse(res);
  if (!res.ok) throw new ApiError(res.status, messageFor(res.status, payload), payload);
  return payload as T;
}
