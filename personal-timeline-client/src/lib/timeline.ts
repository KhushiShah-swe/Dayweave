import { TimelineEntry } from '../types';

export const SOURCES = ['GitHub', 'Spotify', 'YouTube', 'Manual'] as const;
export const sourceLabels: Record<string, string> = {
  GitHub: 'Code',
  Spotify: 'Music',
  YouTube: 'Videos',
  Manual: 'Moments',
};
export const sortEntries = (entries: TimelineEntry[], oldest = false) =>
  [...entries].sort(
    (a, b) => (Date.parse(b.eventDate) - Date.parse(a.eventDate)) * (oldest ? -1 : 1),
  );
export function localDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
export function filterEntries(
  entries: TimelineEntry[],
  query: string,
  source: string,
  from = '',
  to = '',
) {
  const search = query.trim().toLowerCase();
  return entries.filter((entry) => {
    const day = localDate(new Date(entry.eventDate));
    return (
      (source === 'All' || entry.sourceApi === source) &&
      (!from || day >= from) &&
      (!to || day <= to) &&
      [entry.title, entry.description, entry.category, entry.entryType].some((text) =>
        text?.toLowerCase().includes(search),
      )
    );
  });
}
export function safeUrl(value?: string) {
  if (!value) return undefined;
  try {
    const url = new URL(value);
    return ['https:', 'http:'].includes(url.protocol) ? url.href : undefined;
  } catch {
    return undefined;
  }
}
export function recentDays(entries: TimelineEntry[], now = new Date()) {
  return Array.from({ length: 7 }, (_, i) => {
    const date = new Date(now);
    date.setDate(date.getDate() - 6 + i);
    const key = localDate(date);
    return {
      key,
      label: date.toLocaleDateString('en-US', { weekday: 'short' }),
      count: entries.filter((e) => localDate(new Date(e.eventDate)) === key).length,
    };
  });
}
export function downloadEntries(entries: TimelineEntry[]) {
  const blob = new Blob(
    [
      JSON.stringify(
        { app: 'Dayweave', version: 1, exportedAt: new Date().toISOString(), entries },
        null,
        2,
      ),
    ],
    { type: 'application/json' },
  );
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `dayweave-${localDate(new Date())}.json`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
