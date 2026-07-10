// Base URL of the Picknic API. Precedence: PUBLIC_API_BASE > dev/prod
// defaults. Only PUBLIC_-prefixed env vars are inlined by Rsbuild.
const DEV_API_PORT = '8080';
const PROD_DEFAULT = 'https://api.picknic.app';

function devDefault(): string {
  const location = (globalThis as { location?: { hostname?: string } }).location;
  const devBundleHost = location?.hostname;
  const hasDeviceReachableHost = devBundleHost && devBundleHost !== '0.0.0.0';

  if (hasDeviceReachableHost) return `http://${devBundleHost}:${DEV_API_PORT}`;

  // Native has no `location`; the dev build inlines the machine's LAN address
  // (lynx.config.ts) so a device on the same network reaches the local API.
  const lanHost = import.meta.env.PUBLIC_DEV_LAN_HOST as string | undefined;
  if (lanHost) return `http://${lanHost}:${DEV_API_PORT}`;
  return `http://localhost:${DEV_API_PORT}`;
}

function resolveDefault(): string {
  const override = import.meta.env.PUBLIC_API_BASE as string | undefined;
  if (override) return override;
  return import.meta.env.PROD ? PROD_DEFAULT : devDefault();
}

const base = resolveDefault().replace(/\/+$/, '');

export function getApiBase(): string {
  return base;
}

export function apiUrl(path: string): string {
  return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}
