import { FormEvent, useEffect, useRef, useState } from 'react';
import { EntryInput, TimelineEntry } from '../types';
import { useTimeline } from '../context/TimelineContext';
import { localDate, safeUrl } from '../lib/timeline';
import { Icon } from './Icon';
export default function EntryDialog({
  entry,
  onClose,
}: {
  entry?: TimelineEntry;
  onClose: () => void;
}) {
  const { save } = useTimeline();
  const dialog = useRef<HTMLDialogElement>(null);
  const date = entry ? new Date(entry.eventDate) : new Date();
  const [form, setForm] = useState<EntryInput>({
    title: entry?.title || '',
    description: entry?.description || '',
    eventDate: `${localDate(date)}T${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`,
    category: entry?.category || 'Personal',
    entryType: entry?.entryType || 'Memory',
    externalUrl: entry?.externalUrl || '',
    imageUrl: entry?.imageUrl || '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => {
    const el = dialog.current!;
    el.showModal();
    return () => el.close();
  }, []);
  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    if (!form.title.trim()) {
      setError('Give your moment a title.');
      return;
    }
    if (!Number.isFinite(Date.parse(form.eventDate))) {
      setError('Choose a valid date and time.');
      return;
    }
    if (form.externalUrl && !safeUrl(form.externalUrl)) {
      setError('Use a complete http:// or https:// link.');
      return;
    }
    setSaving(true);
    try {
      await save({ ...form, eventDate: new Date(form.eventDate).toISOString() }, entry?.id);
      onClose();
    } catch (e) {
      setError(
        e instanceof Error && !('isAxiosError' in e)
          ? e.message
          : 'Your moment could not be saved. Please try again.',
      );
      setSaving(false);
    }
  }
  const change = (key: keyof EntryInput, value: string) =>
    setForm((prev) => ({ ...prev, [key]: value }));
  return (
    <dialog
      ref={dialog}
      className="editor-dialog"
      aria-labelledby="editor-title"
      onCancel={(e) => {
        e.preventDefault();
        if (!saving) onClose();
      }}
    >
      <form onSubmit={submit}>
        <div className="dialog-heading">
          <span className="eyebrow">MAKE IT A MEMORY</span>
          <button
            type="button"
            className="icon-button"
            aria-label="Close editor"
            onClick={onClose}
            disabled={saving}
          >
            <Icon name="close" />
          </button>
        </div>
        <h2 id="editor-title">{entry ? 'Revisit a moment.' : 'Save a little of today.'}</h2>
        <p className="muted">The small things deserve a place, too.</p>
        <label>
          Title
          <input
            autoFocus
            required
            maxLength={160}
            value={form.title}
            onChange={(e) => change('title', e.target.value)}
            placeholder="What would you like to remember?"
          />
        </label>
        <div className="form-row">
          <label>
            Date and time
            <input
              type="datetime-local"
              required
              value={form.eventDate}
              onChange={(e) => change('eventDate', e.target.value)}
            />
          </label>
          <label>
            Moment type
            <select value={form.entryType} onChange={(e) => change('entryType', e.target.value)}>
              {['Memory', 'Milestone', 'Achievement', 'Activity'].map((v) => (
                <option key={v}>{v}</option>
              ))}
            </select>
          </label>
        </div>
        <label>
          Your story
          <textarea
            rows={4}
            maxLength={5000}
            placeholder="A thought, a lesson, a detail you don't want to forget…"
            value={form.description}
            onChange={(e) => change('description', e.target.value)}
          />
        </label>
        <div className="form-row">
          <label>
            Category
            <input
              maxLength={60}
              value={form.category}
              onChange={(e) => change('category', e.target.value)}
            />
          </label>
          <label>
            Link (optional)
            <input
              type="url"
              placeholder="https://"
              maxLength={2048}
              value={form.externalUrl}
              onChange={(e) => change('externalUrl', e.target.value)}
            />
          </label>
        </div>
        {error && (
          <p role="alert" className="error-banner">
            {error}
          </p>
        )}
        <div className="dialog-footer">
          <button type="button" className="button secondary" onClick={onClose} disabled={saving}>
            Cancel
          </button>
          <button className="button primary" disabled={saving}>
            {saving ? 'Saving…' : entry ? 'Save changes' : 'Save moment'}
            <Icon name="arrow" size={17} />
          </button>
        </div>
      </form>
    </dialog>
  );
}
