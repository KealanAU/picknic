import { request } from './http';

export interface HostEvent {
  id: string;
  code: string;
  name: string;
  tier: string;
  uploadOpensAt: string;
  uploadClosesAt: string;
  revealAt: string;
  uploadOpen: boolean;
  revealed: boolean;
  guestCount: number;
  photoCount: number;
}

export interface CreateHostEventRequest {
  name: string;
  uploadOpensAt: string;
  uploadClosesAt: string;
  revealAt: string;
}

export interface CreatedHostEvent {
  id: string;
  code: string;
  joinSecret: string;
  uploadOpensAt: string;
  uploadClosesAt: string;
  revealAt: string;
}

export interface EventQr {
  code: string;
  joinUrl: string;
  qrPng: string;
}

export interface HostGuest {
  id: string;
  displayName: string;
  email?: string;
  joinedAt: string;
  removed: boolean;
  photos: number;
}

export function listHostEvents(): Promise<HostEvent[]> {
  return request<HostEvent[]>('/api/events');
}

export function createHostEvent(input: CreateHostEventRequest): Promise<CreatedHostEvent> {
  return request<CreatedHostEvent>('/api/events/', { body: input });
}

export function updateHostEvent(id: string, input: CreateHostEventRequest): Promise<HostEvent> {
  return request<HostEvent>(`/api/events/${encodeURIComponent(id)}`, { method: 'PUT', body: input });
}

export function getEventQr(id: string): Promise<EventQr> {
  return request<EventQr>(`/api/events/${encodeURIComponent(id)}/qr`);
}

export function listGuests(eventId: string): Promise<HostGuest[]> {
  return request<HostGuest[]>(`/api/events/${encodeURIComponent(eventId)}/guests/`);
}

export function removeGuest(eventId: string, guestId: string): Promise<void> {
  return request(`/api/events/${encodeURIComponent(eventId)}/guests/${encodeURIComponent(guestId)}`, {
    method: 'DELETE',
  });
}
