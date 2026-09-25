import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTimeline } from '../context/TimelineContext';
import { TimelineEntry } from '../types';
import { downloadEntries, filterEntries, localDate, sortEntries, SOURCES } from '../lib/timeline';
import { Icon } from '../components/Icon';
import EntryCard from '../components/EntryCard';
import EntryDialog from '../components/EntryDialog';
export default function Timeline() {
  const { entries, loading, error, refresh, remove } = useTimeline();
  const [params, setParams] = useSearchParams();
  const [editor, setEditor] = useState<TimelineEntry | 'new' | null>(
    params.get('new') === '1' ? 'new' : null,
  );
  const [query, setQuery] = useState('');
  const [source, setSource] = useState('All');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [oldest, setOldest] = useState(false);
  const [deleting, setDeleting] = useState<TimelineEntry | null>(null);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const confirm = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    if (deleting) confirm.current?.showModal();
  }, [deleting]);
  const filtered = useMemo(
    () => sortEntries(filterEntries(entries, query, source, from, to), oldest),
    [entries, query, source, from, to, oldest],
  );
  const groups = useMemo(
    () =>
      filtered.reduce<Record<string, TimelineEntry[]>>((all, entry) => {
        const key = localDate(new Date(entry.eventDate));
        (all[key] ||= []).push(entry);
        return all;
      }, {}),
    [filtered],
  );
  function closeEditor() {
    setEditor(null);
    if (params.has('new')) {
      const next = new URLSearchParams(params);
      next.delete('new');
      setParams(next, { replace: true });
    }
  }
  function cancelDelete() {
    confirm.current?.close();
    setDeleting(null);
  }
  async function deleteEntry() {
    if (!deleting || busy) return;
    setBusy(true);
    try {
      await remove(deleting.id);
      cancelDelete();
      setNotice('Moment removed.');
    } catch {
      setNotice('Could not delete this moment. Please try again.');
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">YOUR PERSONAL ARCHIVE</p>
          <h1>
            Every day has a story<span>.</span>
          </h1>
          <p className="page-description">The things you made, found, loved, and lived.</p>
        </div>
        <div className="heading-actions">
          <button
            className="button secondary"
            onClick={() => downloadEntries(filtered)}
            disabled={!filtered.length}
          >
            <Icon name="download" size={17} /> Export
          </button>
          <button className="button primary" onClick={() => setEditor('new')}>
            <Icon name="plus" size={18} /> Add a moment
          </button>
        </div>
      </div>
      <section className="filter-panel" aria-label="Timeline filters">
        <div className="filter-top">
          <div className="search-box">
            <Icon name="search" />
            <input
              aria-label="Search timeline"
              placeholder="Find a moment, a song, a little inspiration…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </div>
          <label className="sort-label">
            Sort
            <select
              value={oldest ? 'oldest' : 'newest'}
              onChange={(e) => setOldest(e.target.value === 'oldest')}
            >
              <option value="newest">Newest first</option>
              <option value="oldest">Oldest first</option>
            </select>
          </label>
        </div>
        <div className="filter-bottom">
          <div className="filter-tabs" aria-label="Filter by source">
            {['All', ...SOURCES].map((s) => (
              <button
                key={s}
                aria-pressed={source === s}
                onClick={() => setSource(s)}
                className={source === s ? 'selected' : ''}
              >
                {s !== 'All' && <Icon name={s} size={15} />}{' '}
                {s === 'All' ? 'All moments' : s === 'Manual' ? 'Personal' : s}
              </button>
            ))}
          </div>
          <div className="date-filters">
            <label>
              From
              <input
                type="date"
                value={from}
                max={to || undefined}
                onChange={(e) => setFrom(e.target.value)}
              />
            </label>
            <label>
              To
              <input
                type="date"
                value={to}
                min={from || undefined}
                onChange={(e) => setTo(e.target.value)}
              />
            </label>
          </div>
        </div>
      </section>
      <div className="result-heading">
        <span>
          {filtered.length} {filtered.length === 1 ? 'moment' : 'moments'} in view
        </span>
        {(query || source !== 'All' || from || to) && (
          <button
            className="text-button"
            onClick={() => {
              setQuery('');
              setSource('All');
              setFrom('');
              setTo('');
            }}
          >
            Clear filters
          </button>
        )}
        <button
          className="refresh-button"
          aria-label="Refresh timeline"
          disabled={loading}
          onClick={() => void refresh()}
        >
          <Icon name="refresh" size={15} /> Refresh
        </button>
      </div>
      {notice && (
        <p role="status" className="notice">
          {notice}
        </p>
      )}
      {error && (
        <p role="alert" className="error-banner">
          {error}
        </p>
      )}
      {loading ? (
        <div className="empty-state" role="status">
          Gathering your moments…
        </div>
      ) : filtered.length ? (
        <div className="timeline-groups">
          {Object.entries(groups).map(([day, items]) => (
            <section className="day-group" key={day}>
              <div className="day-marker">
                <span className="timeline-dot" />
                <h2>
                  {new Date(`${day}T12:00:00`).toLocaleDateString('en-US', {
                    weekday: 'long',
                    month: 'long',
                    day: 'numeric',
                    year: 'numeric',
                  })}
                </h2>
                <span>
                  {items.length} {items.length === 1 ? 'moment' : 'moments'}
                </span>
              </div>
              <div className="day-entries">
                {items.map((entry) => (
                  <EntryCard
                    entry={entry}
                    key={entry.id}
                    onEdit={setEditor}
                    onDelete={setDeleting}
                  />
                ))}
              </div>
            </section>
          ))}
        </div>
      ) : (
        <div className="empty-state">
          <Icon name="search" size={32} />
          <h2>{entries.length ? 'No moments found.' : 'A blank page, full of possibility.'}</h2>
          <p>
            {entries.length
              ? 'Try another search or clear your filters.'
              : 'Start with something you want to remember.'}
          </p>
          {!entries.length && (
            <button className="button primary" onClick={() => setEditor('new')}>
              Add your first moment
            </button>
          )}
        </div>
      )}
      {editor && (
        <EntryDialog entry={editor === 'new' ? undefined : editor} onClose={closeEditor} />
      )}
      <dialog
        ref={confirm}
        className="confirm-dialog"
        aria-labelledby="delete-title"
        onCancel={(e) => {
          e.preventDefault();
          if (!busy) cancelDelete();
        }}
      >
        <h2 id="delete-title">Remove this moment?</h2>
        <p>
          “{deleting?.title}” will be removed from your timeline. This cannot be undone.
          {deleting?.sourceApi !== 'Manual' && ' Synced activity may return during the next sync.'}
        </p>
        <div className="dialog-footer">
          <button className="button secondary" disabled={busy} onClick={cancelDelete}>
            Keep moment
          </button>
          <button className="button danger" disabled={busy} onClick={() => void deleteEntry()}>
            {busy ? 'Removing…' : 'Remove moment'}
          </button>
        </div>
      </dialog>
    </>
  );
}
