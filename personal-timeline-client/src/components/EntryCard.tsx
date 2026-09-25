import { TimelineEntry } from '../types';
import { safeUrl, sourceLabels } from '../lib/timeline';
import { Icon } from './Icon';
interface Props {
  entry: TimelineEntry;
  onEdit?: (entry: TimelineEntry) => void;
  onDelete?: (entry: TimelineEntry) => void;
}
export default function EntryCard({ entry, onEdit, onDelete }: Props) {
  const externalUrl = safeUrl(entry.externalUrl);
  return (
    <article className={`entry-card source-${entry.sourceApi.toLowerCase()}`}>
      <span className="source-icon">
        <Icon name={entry.sourceApi} />
      </span>
      <div className="entry-content">
        <div className="entry-meta">
          <span>{entry.sourceApi === 'Manual' ? 'A personal moment' : entry.sourceApi}</span>
          <span className="meta-dot">·</span>
          <time dateTime={entry.eventDate}>
            {new Date(entry.eventDate).toLocaleTimeString('en-US', {
              hour: 'numeric',
              minute: '2-digit',
            })}
          </time>
        </div>
        <h3>{entry.title}</h3>
        <p>{entry.description}</p>
        <div className="entry-bottom">
          <span className="tag">
            {entry.category || sourceLabels[entry.sourceApi] || entry.entryType}
          </span>
          {entry.entryType !== 'Activity' && <span className="entry-kind">{entry.entryType}</span>}
          {externalUrl && (
            <a href={externalUrl} target="_blank" rel="noopener noreferrer" className="source-link">
              View source <Icon name="external" size={13} />
            </a>
          )}
        </div>
      </div>
      {(onEdit || onDelete) && (
        <div className="entry-actions">
          {onEdit && entry.sourceApi === 'Manual' && (
            <button
              className="icon-button"
              aria-label={`Edit ${entry.title}`}
              onClick={() => onEdit(entry)}
            >
              <Icon name="edit" size={16} />
            </button>
          )}
          {onDelete && (
            <button
              className="icon-button"
              aria-label={`Delete ${entry.title}`}
              onClick={() => onDelete(entry)}
            >
              <Icon name="trash" size={16} />
            </button>
          )}
        </div>
      )}
    </article>
  );
}
