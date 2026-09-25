import {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react';
import { EntryInput, TimelineEntry } from '../types';
import { api } from '../lib/api';
import { DEMO_KEY, makeDemoEntries, readDemoEntries } from '../lib/demo';
import { sortEntries } from '../lib/timeline';
import { useAuth } from './AuthContext';
interface Timeline {
  entries: TimelineEntry[];
  loading: boolean;
  error: string;
  refresh: () => Promise<void>;
  save: (input: EntryInput, id?: number) => Promise<void>;
  remove: (id: number) => Promise<void>;
  resetDemo: () => void;
}
const Context = createContext<Timeline | null>(null);
export function TimelineProvider({ children }: { children: ReactNode }) {
  const { user, isDemo } = useAuth();
  const userId = user?.id;
  const [entries, setEntries] = useState<TimelineEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const revision = useRef(0);
  const current = useRef<TimelineEntry[]>([]);
  function commit(next: TimelineEntry[], persist = false) {
    const sorted = sortEntries(next);
    if (persist) {
      try {
        localStorage.setItem(DEMO_KEY, JSON.stringify(sorted));
      } catch {
        throw new Error(
          'Your browser could not save this change. Free some storage and try again.',
        );
      }
    }
    current.current = sorted;
    setEntries(sorted);
  }
  const refresh = useCallback(async () => {
    const version = ++revision.current;
    if (userId === undefined) {
      commit([]);
      setError('');
      setLoading(false);
      return;
    }
    setLoading(true);
    setError('');
    try {
      const data = isDemo
        ? readDemoEntries()
        : (await api.get<TimelineEntry[]>('/api/timeline')).data;
      if (version === revision.current) commit(data);
    } catch {
      if (version === revision.current)
        setError('We could not load your timeline. Check the API connection and try again.');
    } finally {
      if (version === revision.current) setLoading(false);
    }
  }, [userId, isDemo]);
  useEffect(() => {
    void refresh();
    return () => {
      revision.current++;
    };
  }, [refresh]);
  async function save(input: EntryInput, id?: number) {
    if (userId === undefined) throw new Error('Sign in to save a moment.');
    if (!input.title.trim() || !Number.isFinite(Date.parse(input.eventDate)))
      throw new Error('Add a title and a valid date.');
    if (id !== undefined && current.current.find((e) => e.id === id)?.sourceApi !== 'Manual')
      throw new Error('Only manual moments can be edited.');
    const value = { ...input, title: input.title.trim() };
    if (isDemo) {
      const nextId = id ?? Math.max(0, ...current.current.map((e) => e.id)) + 1;
      const entry = { ...value, id: nextId, sourceApi: 'Manual' };
      commit(
        id === undefined
          ? [...current.current, entry]
          : current.current.map((e) => (e.id === id ? entry : e)),
        true,
      );
    } else {
      if (id === undefined) {
        const { data } = await api.post<TimelineEntry>('/api/timeline', value);
        commit([...current.current, data]);
      } else {
        await api.put(`/api/timeline/${id}`, value);
        commit(current.current.map((e) => (e.id === id ? { ...e, ...value } : e)));
      }
    }
  }
  async function remove(id: number) {
    if (userId === undefined) throw new Error('Sign in to delete a moment.');
    if (!isDemo) await api.delete(`/api/timeline/${id}`);
    commit(
      current.current.filter((e) => e.id !== id),
      isDemo,
    );
  }
  function resetDemo() {
    if (isDemo) commit(makeDemoEntries(), true);
  }
  return (
    <Context.Provider value={{ entries, loading, error, refresh, save, remove, resetDemo }}>
      {children}
    </Context.Provider>
  );
}
export function useTimeline() {
  const value = useContext(Context);
  if (!value) throw new Error('TimelineProvider is required');
  return value;
}
