// Native camera boundary.
//
// Lynx has no built-in camera element or module — capture is a *custom* native
// module the host app implements (see app/native/ios & app/native/android),
// exactly like `NativeLocalStorageModule` in api/storage.ts. It is therefore
// absent in Lynx Explorer and web preview, so callers must check
// `isCameraAvailable()` and degrade (e.g. a "camera unavailable" state).
//
// This file is the *only* place that touches `NativeModules.CameraModule`.
// Everything above it (composables, screens) works with the plain
// `CapturedPhoto` shape and never sees the bridge.

export interface CaptureOptions {
  /** JPEG quality 0..1. Default 0.9. */
  quality?: number;
  /** Which camera to open. Default 'back'. */
  facing?: 'front' | 'back';
}

export interface CapturedPhoto {
  /** Raw JPEG bytes of the capture. */
  bytes: ArrayBuffer;
  width: number;
  height: number;
  mime: string;
}

// The wire shape the native module returns through its callback. Kept internal —
// callers never see base64.
interface NativeCaptureResult {
  base64?: string;
  width?: number;
  height?: number;
  mime?: string;
  /** Non-empty when capture failed or was cancelled. */
  error?: string;
}

interface NativeCameraModule {
  capture(
    options: { quality: number; facing: string },
    callback: (result: NativeCaptureResult) => void,
  ): unknown; // some hosts also return a Promise — we support both
}

declare const NativeModules: Record<string, any> | undefined;

function module(): NativeCameraModule | null {
  try {
    return typeof NativeModules !== 'undefined'
      ? (NativeModules.CameraModule as NativeCameraModule) ?? null
      : null;
  } catch {
    return null;
  }
}

export function isCameraAvailable(): boolean {
  const mod = module();
  return !!mod && typeof mod.capture === 'function';
}

/** User cancelled the camera (distinct from a real failure). */
export class CameraCancelled extends Error {
  constructor() {
    super('Capture cancelled');
    this.name = 'CameraCancelled';
  }
}

export async function capturePhoto(options: CaptureOptions = {}): Promise<CapturedPhoto> {
  const mod = module();
  if (!mod || typeof mod.capture !== 'function') {
    throw new Error('Camera is not available in this runtime.');
  }

  const args = {
    quality: clamp01(options.quality ?? 0.9),
    facing: options.facing ?? 'back',
  };

  const result = await new Promise<NativeCaptureResult>((resolve, reject) => {
    try {
      const maybe = mod.capture(args, (r) => resolve(r ?? {}));
      // Host may resolve through a returned Promise instead of the callback.
      if (maybe && typeof (maybe as any).then === 'function') {
        (maybe as Promise<NativeCaptureResult>).then((r) => resolve(r ?? {}), reject);
      }
    } catch (e) {
      reject(e);
    }
  });

  if (result.error) {
    if (/cancel/i.test(result.error)) throw new CameraCancelled();
    throw new Error(result.error);
  }
  if (!result.base64) throw new Error('Camera returned no image data.');

  return {
    bytes: base64ToArrayBuffer(result.base64),
    width: result.width ?? 0,
    height: result.height ?? 0,
    mime: result.mime ?? 'image/jpeg',
  };
}

/** A `data:` URI for a captured photo, for previewing in an <image>. */
export function toDataUri(photo: CapturedPhoto): string {
  return `data:${photo.mime};base64,${arrayBufferToBase64(photo.bytes)}`;
}

function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
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

// Lynx's runtime doesn't guarantee `atob`, so decode base64 ourselves.
const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
function base64ToArrayBuffer(b64: string): ArrayBuffer {
  const clean = b64.replace(/[^A-Za-z0-9+/]/g, '');
  const pad = clean.endsWith('==') ? 2 : clean.endsWith('=') ? 1 : 0;
  const len = (clean.length / 4) * 3 - pad;
  const out = new Uint8Array(len);
  let p = 0;
  for (let i = 0; i < clean.length; i += 4) {
    const n =
      (B64.indexOf(clean[i]) << 18) |
      (B64.indexOf(clean[i + 1]) << 12) |
      (B64.indexOf(clean[i + 2]) << 6) |
      B64.indexOf(clean[i + 3]);
    if (p < len) out[p++] = (n >> 16) & 0xff;
    if (p < len) out[p++] = (n >> 8) & 0xff;
    if (p < len) out[p++] = n & 0xff;
  }
  return out.buffer;
}
