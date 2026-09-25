export type Source = 'GitHub' | 'Spotify' | 'YouTube' | 'Manual';
export interface TimelineEntry {
  id: number;
  title: string;
  description: string;
  eventDate: string;
  entryType: string;
  category: string;
  imageUrl?: string;
  externalUrl?: string;
  sourceApi: string;
}
export type EntryInput = Omit<TimelineEntry, 'id' | 'sourceApi'>;
export interface Connection {
  id: number;
  apiProvider: string;
  isActive: boolean;
  lastSyncAt: string;
}
export interface User {
  id: number;
  displayName: string;
}
