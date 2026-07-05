// Base URL of the Picknic API. Precedence: setApiBase() > PUBLIC_API_BASE >
// dev/prod defaults. Only PUBLIC_-prefixed env vars are inlined by Rsbuild.
const DEV_DEFAULT = 'http://localhost:8080';
const PROD_DEFAULT = 'https://api.picknic.app';

function resolveDefault(): string {
  const override = import.meta.env.PUBLIC_API_BASE as string | undefined;
  if (override) return override;
  return import.meta.env.PROD ? PROD_DEFAULT : DEV_DEFAULT;
}

let base = resolveDefault().replace(/\/+$/, '');

export function setApiBase(url: string): void {
  base = url.replace(/\/+$/, '');
}

export function getApiBase(): string {
  return base;
}

export function apiUrl(path: string): string {
  return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}
