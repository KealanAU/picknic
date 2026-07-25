// Native camera boundary — delegates to @vyui/camera, the only place
// that touches NativeModules.CameraModule. The module is absent in Explorer /
// Lynx Go / web preview, so callers must check isCameraAvailable() and degrade.

// Static import (not dynamic): lazy chunks load through Lynx's own chunk
// loader, which is unreliable in the web preview.
import {
  createCameraModule,
  getCameraInstallStatus,
  getNativeCameraModule,
  invokeCameraViewMethod,
  type CameraInstallStatus,
  type CameraModuleClient,
  type CapturePhotoOptions,
  type PhotoFile,
} from '@vyui/camera';
import { createMockCameraModule } from '@vyui/camera/mock';
import { DEV_SAMPLE_JPEG_BASE64 } from './devSamplePhoto';

export interface CaptureOptions {
  quality?: number; // JPEG quality 0..1, default 0.9
  facing?: 'front' | 'back';
}

export interface CapturedPhoto {
  bytes: ArrayBuffer;
  width: number;
  height: number;
  mime: string;
}

let nativeAdapter: CameraModuleClient | null = null;
let devMockAdapter: CameraModuleClient | null = null;

// Resolved lazily and re-probed until found: some hosts register
// NativeModules after the JS bundle evaluates, so probing once at module
// eval would latch "unavailable" for the whole session. Only a real native
// adapter is cached; the DEV mock is a per-call fallback so a late-arriving
// native module still wins.
function getAdapter(): CameraModuleClient | null {
  if (!nativeAdapter) {
    nativeAdapter = createCameraModule({ optional: true });
  }
  if (nativeAdapter) return nativeAdapter;

  // DEV-only stand-in: Explorer, Lynx Go, and the web preview have no
  // CameraModule, so the package mock returns the embedded sample JPEG and
  // the whole capture->upload flow stays exercisable. Prod builds drop this
  // branch with import.meta.env.DEV.
  if (import.meta.env.DEV) {
    devMockAdapter ??= createMockCameraModule({
      photo: {
        path: 'mock://picknic/dev-sample.jpg',
        width: 320,
        height: 280,
        mime: 'image/jpeg',
        base64: DEV_SAMPLE_JPEG_BASE64,
      },
    });
    return devMockAdapter;
  }
  return null;
}

export function isCameraAvailable(): boolean {
  return getAdapter() !== null;
}

// Why the camera is (un)available — render this on device when debugging;
// console logs are invisible on Lynx Go without DevTool attached.
export function cameraInstallStatus(): CameraInstallStatus {
  return getCameraInstallStatus();
}

export class CameraCancelled extends Error {
  constructor() {
    super('Capture cancelled');
    this.name = 'CameraCancelled';
  }
}

export async function capturePhoto(options: CaptureOptions = {}): Promise<CapturedPhoto> {
  const adapter = getAdapter();
  if (!adapter) throw new Error('Camera is not available in this runtime.');

  // includeBase64 is off by default in the package; putBlob needs the bytes
  // in JS, and JS can't read the returned temp-file path.
  const captureOptions: CapturePhotoOptions = { includeBase64: true };
  if (options.quality !== undefined) captureOptions.quality = options.quality;
  if (options.facing !== undefined) captureOptions.facing = options.facing;

  let photo;
  try {
    photo = await adapter.capturePhoto(captureOptions);
  } catch (e) {
    if (isCancel(e)) throw new CameraCancelled();
    throw e instanceof Error ? e : new Error(String(e));
  }

  if (!photo.base64) throw new Error('Camera returned no image data.');

  return {
    bytes: base64ToArrayBuffer(photo.base64),
    width: photo.width ?? 0,
    height: photo.height ?? 0,
    mime: photo.mime ?? 'image/jpeg',
  };
}

// Captures through a rendered <camera-view> (embedded live preview) instead
// of the system camera sheet. Only valid when install status is 'installed' —
// the element registers alongside the native module. base64 is requested
// because putBlob needs the bytes in JS; JS can't read the temp-file path.
export async function captureFromView(selector: string, options: CaptureOptions = {}): Promise<CapturedPhoto> {
  const photo = await invokeCameraViewMethod<PhotoFile>(selector, 'capturePhoto', {
    quality: options.quality ?? 0.9,
    includeBase64: true,
  });
  if (!photo.base64) throw new Error('Camera returned no image data.');
  return {
    bytes: base64ToArrayBuffer(photo.base64),
    width: photo.width ?? 0,
    height: photo.height ?? 0,
    mime: photo.mime ?? 'image/jpeg',
  };
}

// pickPhoto is optional in the native contract — host builds that predate it
// still register a valid module, so hide the library button when it's absent.
// No native module at all means the DEV mock, which does implement it.
export function isLibraryPickAvailable(): boolean {
  if (!getAdapter()) return false;
  const native = getNativeCameraModule<{ pickPhoto?: unknown }>();
  return !native || typeof native.pickPhoto === 'function';
}

export async function pickFromLibrary(options: CaptureOptions = {}): Promise<CapturedPhoto> {
  const adapter = getAdapter();
  if (!adapter) throw new Error('The photo library is not available in this runtime.');

  let photo;
  try {
    photo = await adapter.pickPhoto({ quality: options.quality ?? 0.9, includeBase64: true });
  } catch (e) {
    if (isCancel(e)) throw new CameraCancelled();
    throw e instanceof Error ? e : new Error(String(e));
  }
  if (!photo.base64) throw new Error('Camera returned no image data.');

  return {
    bytes: base64ToArrayBuffer(photo.base64),
    width: photo.width ?? 0,
    height: photo.height ?? 0,
    mime: photo.mime ?? 'image/jpeg',
  };
}

// Native rejects carry a ChimeraCameraError `code` (capture/cancelled,
// library/cancelled); older hosts only say it in the message.
function isCancel(e: unknown): boolean {
  const code = (e as { code?: unknown })?.code;
  const message = e instanceof Error ? e.message : String(e);
  return /cancel/i.test(`${typeof code === 'string' ? code : ''} ${message}`);
}

export function toDataUri(photo: CapturedPhoto): string {
  return `data:${photo.mime};base64,${arrayBufferToBase64(photo.bytes)}`;
}

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let out = '';
  for (let i = 0; i < bytes.length; i += 3) {
    const b0 = bytes[i];
    const b1 = i + 1 < bytes.length ? bytes[i + 1] : 0;
    const b2 = i + 2 < bytes.length ? bytes[i + 2] : 0;
    out += B64[b0 >> 2];
    out += B64[((b0 & 3) << 4) | (b1 >> 4)];
    out += i + 1 < bytes.length ? B64[((b1 & 15) << 2) | (b2 >> 6)] : '=';
    out += i + 2 < bytes.length ? B64[b2 & 63] : '=';
  }
  return out;
}

// Lynx's runtime doesn't guarantee atob/btoa, so code base64 ourselves.
const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';

// Missing/padding positions decode as 0, never -1: `B64.indexOf(undefined)`
// is -1, and OR-ing -1 corrupted the final 1-2 bytes of every padded payload
// (JPEG tails decoded as 0xFF), which broke most real captures.
function sextet(c: string | undefined): number {
  if (c === undefined) return 0;
  const v = B64.indexOf(c);
  return v < 0 ? 0 : v;
}

function base64ToArrayBuffer(b64: string): ArrayBuffer {
  // Strip whitespace/padding; the byte length falls out of the remaining
  // character count (4 chars -> 3 bytes, 3 -> 2, 2 -> 1).
  const body = b64.replace(/[^A-Za-z0-9+/]/g, '');
  const len = Math.floor((body.length * 3) / 4);
  const out = new Uint8Array(len);
  let p = 0;
  for (let i = 0; i < body.length; i += 4) {
    const n =
      (sextet(body[i]) << 18) |
      (sextet(body[i + 1]) << 12) |
      (sextet(body[i + 2]) << 6) |
      sextet(body[i + 3]);
    if (p < len) out[p++] = (n >> 16) & 0xff;
    if (p < len) out[p++] = (n >> 8) & 0xff;
    if (p < len) out[p++] = n & 0xff;
  }
  return out.buffer;
}
