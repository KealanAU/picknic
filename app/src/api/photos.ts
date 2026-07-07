// Photo upload + reveal, over the http wrapper. Upload is a three-step handshake
// so the API never proxies image bytes: createUpload (mint SAS) -> putBlob (PUT
// straight to Azure) -> completeUpload (register the blob + caption).
import { request } from './http';

export interface UploadTarget {
  uploadUrl: string;
  blobPath: string;
}

export function createUpload(eventId: string, guestToken: string): Promise<UploadTarget> {
  return request<UploadTarget>(`/api/events/${eventId}/uploads`, {
    method: 'POST',
    token: guestToken,
  });
}

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

export interface FilmStockInfo {
  id: string;
  displayName: string;
}

export interface PrintStyleInfo {
  id: string;
  displayName: string;
}

export function listFilmStocks(): Promise<FilmStockInfo[]> {
  return request<FilmStockInfo[]>('/api/film/stocks', { auth: false });
}

export function listPrintStyles(): Promise<PrintStyleInfo[]> {
  return request<PrintStyleInfo[]>('/api/film/prints', { auth: false });
}

export interface DevelopResult {
  photoId: string;
  stock: string;
  print: string;
  developed: boolean;
}

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
  url: string | null;
}

export type PhotosResponse =
  | { revealed: false; revealAt: string }
  | { revealed: true; photos: RevealedPhoto[] };

export function getPhotos(eventId: string): Promise<PhotosResponse> {
  return request<PhotosResponse>(`/api/events/${eventId}/photos`, { auth: false });
}
