// Native camera boundary — the only place that touches NativeModules.CameraModule.
// Lynx has no built-in camera; the host app implements this module (see
// app/native/ios & app/native/android), so it's absent in Explorer/web preview.
// Callers must check isCameraAvailable() and degrade.

// Static import (not dynamic): lazy chunks load through Lynx's own chunk
// loader, which is unreliable in the web preview.
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

interface NativeCaptureResult {
  base64?: string;
  width?: number;
  height?: number;
  mime?: string;
  error?: string; // non-empty when capture failed or was cancelled
}

interface NativeCameraModule {
  capture(
    options: { quality: number; facing: string },
    callback: (result: NativeCaptureResult) => void,
  ): unknown;
}

declare const NativeModules: Record<string, any> | undefined;

function cameraModule(): NativeCameraModule | null {
  try {
    return typeof NativeModules !== 'undefined'
      ? (NativeModules.CameraModule as NativeCameraModule) ?? null
      : null;
  } catch {
    return null;
  }
}

// DEV-only stand-in: Explorer and the web preview have no CameraModule, so the
// fake returns an embedded sample JPEG and the whole capture->upload flow stays
// exercisable. Prod builds drop this branch with import.meta.env.DEV.
function devFakeAvailable(): boolean {
  return !!import.meta.env.DEV;
}

export function isCameraAvailable(): boolean {
  const mod = cameraModule();
  return (!!mod && typeof mod.capture === 'function') || devFakeAvailable();
}

export class CameraCancelled extends Error {
  constructor() {
    super('Capture cancelled');
    this.name = 'CameraCancelled';
  }
}

export async function capturePhoto(options: CaptureOptions = {}): Promise<CapturedPhoto> {
  const mod = cameraModule();
  if (!mod || typeof mod.capture !== 'function') {
    if (devFakeAvailable()) {
      return {
        bytes: base64ToArrayBuffer(DEV_SAMPLE_JPEG_BASE64),
        width: 320,
        height: 280,
        mime: 'image/jpeg',
      };
    }
    throw new Error('Camera is not available in this runtime.');
  }

  const args = {
    quality: clamp01(options.quality ?? 0.9),
    facing: options.facing ?? 'back',
  };

  const result = await new Promise<NativeCaptureResult>((resolve, reject) => {
    try {
      // Host may resolve via the callback or a returned Promise; support both.
      const captureReturn = mod.capture(args, (r) => resolve(r ?? {}));
      if (captureReturn && typeof (captureReturn as any).then === 'function') {
        (captureReturn as Promise<NativeCaptureResult>).then((r) => resolve(r ?? {}), reject);
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

// Lynx's runtime doesn't guarantee atob/btoa, so code base64 ourselves.
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
