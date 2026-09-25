import { TimelineEntry } from '../types';

export const DEMO_KEY = 'dayweave.demo.entries.v1';
/** Fictional activity only. Dates follow the current week so the demo remains useful. */
export function makeDemoEntries(now = new Date()): TimelineEntry[] {
  const samples = [
    [
      'GitHub',
      'A small commit. A better experience.',
      'Refined keyboard navigation and polished the timeline filters.',
      'Development',
      'Achievement',
      0,
      10,
    ],
    [
      'Spotify',
      'The soundtrack to a slow morning',
      'A little instrumental jazz, a cup of coffee, and a fresh start.',
      'Music',
      'Activity',
      0,
      8,
    ],
    [
      'Manual',
      'Made room for a new idea',
      'Sketched a tiny side project in my notebook. Sometimes that is all it takes.',
      'Personal',
      'Memory',
      1,
      19,
    ],
    [
      'YouTube',
      'Learning something worth keeping',
      'Watched a lesson on designing accessible interfaces and saved a few takeaways.',
      'Learning',
      'Activity',
      1,
      14,
    ],
    [
      'GitHub',
      'From first sketch to first release',
      'Published the first version of a weekend project.',
      'Development',
      'Milestone',
      1,
      11,
    ],
    [
      'Spotify',
      'One more song before calling it a day',
      'An evening playlist for the walk home.',
      'Music',
      'Activity',
      2,
      18,
    ],
    [
      'Manual',
      'A walk without a destination',
      'Left the headphones at home and noticed a little more of the neighborhood.',
      'Personal',
      'Memory',
      2,
      16,
    ],
    [
      'GitHub',
      'The test finally turned green',
      'Tracked down an edge case in date filtering and added a regression test.',
      'Development',
      'Achievement',
      3,
      15,
    ],
    [
      'YouTube',
      'A new perspective on system design',
      'Explored background workers, retries, and what happens when an API is unavailable.',
      'Learning',
      'Activity',
      4,
      17,
    ],
    [
      'Spotify',
      'Deep focus, on repeat',
      'A quiet electronic mix for an afternoon of building.',
      'Music',
      'Activity',
      4,
      12,
    ],
    [
      'GitHub',
      'Something new starts here',
      'Created a repository and wrote down the problem I wanted to solve.',
      'Development',
      'Milestone',
      5,
      10,
    ],
    [
      'Manual',
      'A good week, in small moments',
      'Wrote down three things I learned and one thing I want to try next.',
      'Personal',
      'Memory',
      6,
      18,
    ],
  ];
  return samples.map(
    ([sourceApi, title, description, category, entryType, daysAgo, hour], index) => {
      const date = new Date(now);
      date.setDate(date.getDate() - Number(daysAgo));
      date.setHours(Number(hour), 15, 0, 0);
      // Keep today's samples at or before the current moment.
      const eventDate = new Date(
        Math.min(date.getTime(), now.getTime() - index * 60000),
      ).toISOString();
      return {
        id: index + 1,
        sourceApi: String(sourceApi),
        title: String(title),
        description: String(description),
        category: String(category),
        entryType: String(entryType),
        eventDate,
      };
    },
  );
}
export function readDemoEntries(): TimelineEntry[] {
  try {
    const raw = localStorage.getItem(DEMO_KEY);
    if (!raw) return makeDemoEntries();
    const parsed: unknown = JSON.parse(raw);
    if (
      !Array.isArray(parsed) ||
      !parsed.every(
        (e) =>
          typeof e?.id === 'number' &&
          typeof e?.title === 'string' &&
          typeof e?.description === 'string' &&
          typeof e?.sourceApi === 'string' &&
          typeof e?.category === 'string' &&
          typeof e?.entryType === 'string' &&
          Number.isFinite(Date.parse(e?.eventDate)),
      )
    )
      return makeDemoEntries();
    return parsed;
  } catch {
    return makeDemoEntries();
  }
}
