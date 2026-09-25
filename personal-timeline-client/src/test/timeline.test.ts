import { describe, expect, it } from 'vitest';
import { makeDemoEntries, readDemoEntries, DEMO_KEY } from '../lib/demo';
import { filterEntries, recentDays, safeUrl, sortEntries, localDate } from '../lib/timeline';
import { userFromToken } from '../lib/session';
const date = new Date('2026-09-24T22:00:00Z');
describe('timeline behavior', () => {
  it('combines source, search, and inclusive local calendar dates', () => {
    const entries = makeDemoEntries(date);
    const target = entries[0];
    const day = localDate(new Date(target.eventDate));
    expect(filterEntries(entries, '  KEYBOARD  ', 'GitHub', day, day)).toEqual([target]);
    expect(filterEntries(entries, 'keyboard', 'Spotify', day, day)).toEqual([]);
    expect(filterEntries(entries, '', 'All', '2020-01-01', '2020-01-02')).toEqual([]);
  });
  it('sorts without mutating the supplied array', () => {
    const entries = makeDemoEntries(date).reverse();
    const original = [...entries];
    expect(sortEntries(entries)[0].id).toBe(1);
    expect(entries).toEqual(original);
    expect(sortEntries(entries, true)[0].id).toBe(12);
  });
  it('counts actual activity in seven local calendar days', () => {
    const days = recentDays(makeDemoEntries(date), date);
    expect(days).toHaveLength(7);
    expect(days.reduce((sum, d) => sum + d.count, 0)).toBe(12);
  });
  it('recovers corrupted storage while preserving an intentionally empty timeline', () => {
    localStorage.setItem(DEMO_KEY, '{broken');
    expect(readDemoEntries()).toHaveLength(12);
    localStorage.setItem(DEMO_KEY, '[]');
    expect(readDemoEntries()).toEqual([]);
    localStorage.setItem(DEMO_KEY, '[{"id":1}]');
    expect(readDemoEntries()).toHaveLength(12);
  });
  it('blocks executable links', () => {
    expect(safeUrl('javascript:alert(1)')).toBeUndefined();
    expect(safeUrl('data:text/html,hello')).toBeUndefined();
    expect(safeUrl('https://github.com')).toBe('https://github.com/');
  });
  it('rejects expired and malformed sessions', () => {
    const token = (payload: object) => `e30.${btoa(JSON.stringify(payload))}.signature`;
    expect(userFromToken('broken')).toBeNull();
    expect(userFromToken(token({ sub: '1', exp: 1 }))).toBeNull();
    expect(userFromToken(token({ sub: '0', exp: Date.now() / 1000 + 3600 }))).toBeNull();
    expect(userFromToken(token({ sub: '1', name: 'Test', exp: Date.now() / 1000 + 3600 }))).toEqual(
      { id: 1, displayName: 'Test' },
    );
  });
});
