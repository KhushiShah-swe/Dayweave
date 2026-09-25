import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTimeline } from '../context/TimelineContext';
import { recentDays, SOURCES, sourceLabels, localDate } from '../lib/timeline';
import { Icon } from '../components/Icon';
import EntryCard from '../components/EntryCard';
export default function Overview() {
  const { user, isDemo } = useAuth();
  const { entries, loading, error, refresh } = useTimeline();
  const days = recentDays(entries);
  const total = days.reduce((sum, d) => sum + d.count, 0);
  const dateLabel = new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
  });
  const activeDays = new Set(entries.map((e) => localDate(new Date(e.eventDate)))).size;
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">{dateLabel}</p>
          <h1>
            Your days, connected<span>.</span>
          </h1>
          <p className="page-description">
            {isDemo ? 'Welcome, Explorer.' : `Welcome back, ${user?.displayName}.`} A little code, a
            little music, a life in between.
          </p>
        </div>
        <Link to="/timeline?new=1" className="button primary">
          <Icon name="plus" size={18} /> Add a moment
        </Link>
      </div>
      <section className="intro-banner">
        <div className="intro-copy">
          <span className="eyebrow">THE BIG PICTURE</span>
          <h2>
            Different parts of life.
            <br />
            One place to keep them.
          </h2>
          <p>
            Your work, your discoveries, and the moments
            <br className="desktop-break" /> that make an ordinary day a little more yours.
          </p>
          <Link to="/timeline">
            Explore your timeline <Icon name="arrow" size={18} />
          </Link>
        </div>
        <div className="weave-art" aria-hidden="true">
          <span className="art-orbit orbit-one" />
          <span className="art-orbit orbit-two" />
          <span className="art-orbit orbit-three" />
          <div className="art-center">
            d<span>↗</span>
          </div>
          <span className="art-token token-code">
            <Icon name="GitHub" size={25} />
          </span>
          <span className="art-token token-music">
            <Icon name="Spotify" size={25} />
          </span>
          <span className="art-token token-moment">
            <Icon name="Manual" size={24} />
          </span>
          <span className="art-dot dot-one" />
          <span className="art-dot dot-two" />
        </div>
      </section>
      {error && (
        <div role="alert" className="error-banner">
          {error}{' '}
          <button className="text-button" onClick={() => void refresh()}>
            Try again
          </button>
        </div>
      )}
      <section className="stats-grid" aria-label="Activity summary">
        {[
          {
            label: 'Moments collected',
            value: entries.length,
            icon: 'timeline',
            detail: 'Across your whole timeline',
          },
          { label: 'This week', value: total, icon: 'calendar', detail: 'In the last seven days' },
          {
            label: 'Active days',
            value: activeDays,
            icon: 'Manual',
            detail: 'Days with something to keep',
          },
          {
            label: 'Activity sources',
            value: new Set(entries.map((e) => e.sourceApi)).size,
            icon: 'link',
            detail: isDemo ? 'From fictional sample activity' : 'Represented in your timeline',
          },
        ].map((stat) => (
          <article className="stat-card" key={stat.label}>
            <div>
              <span>{stat.label}</span>
              <Icon name={stat.icon} size={19} />
            </div>
            <strong>{loading ? '—' : String(stat.value).padStart(2, '0')}</strong>
            <small>{stat.detail}</small>
          </article>
        ))}
      </section>
      <div className="overview-grid">
        <section>
          <div className="section-heading">
            <div>
              <h2>Recently, in your world</h2>
              <p>The little things, brought together.</p>
            </div>
            <Link className="text-link" to="/timeline">
              View all <Icon name="arrow" size={16} />
            </Link>
          </div>
          <div className="recent-list">
            {loading ? (
              <p className="empty-state" role="status">
                Gathering your moments…
              </p>
            ) : entries.length ? (
              entries.slice(0, 4).map((entry) => (
                <div key={entry.id}>
                  <div className="recent-date">
                    {new Date(entry.eventDate).toLocaleDateString('en-US', {
                      month: 'short',
                      day: 'numeric',
                    })}
                  </div>
                  <EntryCard entry={entry} />
                </div>
              ))
            ) : (
              <div className="empty-state">
                <Icon name="Manual" size={30} />
                <h3>Your story starts here.</h3>
                <p>Add a moment or connect an app to begin.</p>
                <Link className="text-link" to="/timeline?new=1">
                  Save your first moment →
                </Link>
              </div>
            )}
          </div>
        </section>
        <aside className="insights">
          <section className="panel rhythm">
            <div className="section-heading">
              <h2>Your weekly rhythm</h2>
              <span className="small-badge">7 days</span>
            </div>
            <p className="muted">Every little moment adds up.</p>
            <div
              className="bar-chart"
              role="img"
              aria-label={days.map((d) => `${d.label}: ${d.count} activities`).join(', ')}
            >
              {days.map((day, i) => (
                <div className="bar-column" key={day.key}>
                  <span className="bar-value">{day.count}</span>
                  <div className="bar-track">
                    <div
                      className={`bar ${i === 6 ? 'current' : ''}`}
                      style={{
                        height: `${day.count ? Math.max(7, (day.count / Math.max(1, ...days.map((d) => d.count))) * 100) : 0}%`,
                      }}
                    />
                  </div>
                  <span>{day.label.slice(0, 1)}</span>
                </div>
              ))}
            </div>
            <div className="chart-caption">
              <span className="legend-dot" />
              Activity captured each day
            </div>
          </section>
          <section className="panel source-panel">
            <div className="section-heading">
              <h2>Your activity mix</h2>
              <Icon name="link" size={18} />
            </div>
            {SOURCES.map((source) => (
              <div className={`source-row source-${source.toLowerCase()}`} key={source}>
                <span className="source-icon">
                  <Icon name={source} size={17} />
                </span>
                <div>
                  <strong>{source === 'Manual' ? 'Personal moments' : source}</strong>
                  <small>{sourceLabels[source]}</small>
                </div>
                <span className="source-count">
                  {entries.filter((e) => e.sourceApi === source).length}
                </span>
              </div>
            ))}
            <Link to="/connections" className="connections-link">
              {isDemo ? 'Explore connections' : 'Manage connections'}
              <Icon name="arrow" size={16} />
            </Link>
          </section>
        </aside>
      </div>
    </>
  );
}
