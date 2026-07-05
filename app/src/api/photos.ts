// Photo upload + reveal endpoints, built on the http wrapper. This is the
// *transport* concern only — it knows nothing about the camera or how bytes were
// produced, so a photo picked from the library uploads through the exact same path.
//
// Upload is a three-step handshake so the API never proxies image bytes:
//   1. createUpload  → a short-lived, write-only Azure SAS URL + server-named blob path
//   2. putBlob       → PUT the bytes straight to blob storage (bypasses our API)
//   3. completeUpload→ tell the API the blob landed (also carries the caption)
import { request } from './http';

export interface UploadTarget {
  uploadUrl: string;
  blobPath: string;
}

/** Step 1: mint a SAS upload target for this guest. Requires the guest JWT. */
export function createUpload(eventId: string, guestToken: string): Promise<UploadTarget> {
  return request<UploadTarget>(`/api/events/${eventId}/uploads`, {
    method: 'POST',
    token: guestToken,
  });
}

/** Step 2: PUT raw bytes to the SAS URL. Goes to Azure directly, not our API. */
export async function putBlob(
  uploadUrl: string,
  bytes: ArrayBuffer,
  mime = 'image/jpeg',
): Promise<void> {
  const res = await fetch(uploadUrl, {
    method: 'PUT',
    headers: {
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': mime,
    },
    body: bytes,
  });
  if (!res.ok) {
    throw new Error(`Blob upload failed (${res.status})`);
  }
}

/** Step 3: register the landed blob (idempotent server-side) and attach a caption. */
export function completeUpload(
  eventId: string,
  blobPath: string,
  caption: string | null,
  guestToken: string,
): Promise<{ id?: string }> {
  return request(`/api/events/${eventId}/uploads/complete`, {
    method: 'POST',
    token: guestToken,
    body: { blobPath, caption },
  });
}

// --- Reveal + film stocks ---------------------------------------------------

export interface FilmStockInfo {
  id: string;
  displayName: string;
}

export interface PrintStyleInfo {
  id: string;
  displayName: string;
}

/** The selectable film "rolls" for a picker. Public. */
export function listFilmStocks(): Promise<FilmStockInfo[]> {
  return request<FilmStockInfo[]>('/api/film/stocks', { auth: false });
}

/** The selectable instant-print frames (includes "none"). Public. */
export function listPrintStyles(): Promise<PrintStyleInfo[]> {
  return request<PrintStyleInfo[]>('/api/film/prints', { auth: false });
}

export interface DevelopResult {
  photoId: string;
  stock: string;
  print: string;
  developed: boolean;
}

/** Host-only: develop one photo with a stock + optional print frame. */
export function developPhoto(
  eventId: string,
  photoId: string,
  stock: string,
  print = 'none',
): Promise<DevelopResult> {
  return request<DevelopResult>(`/api/events/${eventId}/photos/${photoId}/develop`, {
    method: 'POST',
    query: { stock, print },
  });
}

export interface RevealedPhoto {
  id: string;
  caption: string | null;
  uploadedByGuestId: string;
  uploadedBy?: string;
  /** SAS read URL — serves the developed (film-look) photo when one exists. */
  url: string | null;
}

export type PhotosResponse =
  | { revealed: false; revealAt: string }
  | { revealed: true; photos: RevealedPhoto[] };

/** The developed roll, once RevealAt passes. Public read. */
export function getPhotos(eventId: string): Promise<PhotosResponse> {
  return request<PhotosResponse>(`/api/events/${eventId}/photos`, { auth: false });
}
