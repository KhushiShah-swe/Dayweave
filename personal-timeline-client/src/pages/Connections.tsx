import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTimeline } from '../context/TimelineContext';
import { api, API_URL } from '../lib/api';
import { Connection } from '../types';
import { Icon } from '../components/Icon';
const providers = [
  {
    name: 'GitHub',
    title: 'The things you build.',
    description: 'Bring repository events, pushes, and discoveries into your timeline.',
  },
  {
    name: 'Spotify',
    title: 'The soundtrack of your days.',
    description: 'Keep a record of recently played tracks and the music in between.',
  },
  {
    name: 'YouTube',
    title: 'A little more curiosity.',
    description: 'Collect liked videos, new discoveries, and things worth learning.',
  },
];
export default function Connections() {
  const { isDemo } = useAuth();
  const { refresh, resetDemo, entries } = useTimeline();
  const [params, setParams] = useSearchParams();
  const [connections, setConnections] = useState<Connection[]>([]);
  const [loading, setLoading] = useState(!isDemo);
  const [busy, setBusy] = useState('');
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');
  const [pending, setPending] = useState('');
  async function load() {
    setLoading(true);
    try {
      const { data } = await api.get<Connection[]>('/api/connections');
      setConnections(data);
    } catch {
      setError('Could not load connections. Check that the API is running.');
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    if (!isDemo) void load();
  }, [isDemo]);
  useEffect(() => {
    if (params.has('sync')) {
      if (params.get('sync') === 'success')
        setNotice('Connection added. You can now sync your activity.');
      else setError('The app could not connect. Check your provider configuration and try again.');
      setParams({}, { replace: true });
    }
  }, [params, setParams]);
  async function act(name: string, action: 'connect' | 'sync' | 'disconnect') {
    if (isDemo) {
      setNotice(
        'You are exploring sample connections. Sign in with GitHub to connect your own accounts.',
      );
      return;
    }
    setBusy(name);
    setError('');
    setNotice('');
    try {
      if (action === 'connect') {
        if (name === 'GitHub') {
          window.location.assign(`${API_URL}/login/github`);
          return;
        }
        const { data } = await api.get<{ redirectUrl: string }>(`/api/connect/${name}`, {
          withCredentials: true,
        });
        const url = new URL(data.redirectUrl);
        const host = name === 'Spotify' ? 'accounts.spotify.com' : 'accounts.google.com';
        if (url.protocol !== 'https:' || url.hostname !== host)
          throw new Error('Unexpected provider URL');
        window.location.assign(url.href);
        return;
      }
      if (action === 'sync') {
        const { data } = await api.post<{ count: number }>(`/api/sync/${name}`);
        setNotice(`${name}: ${data.count} new ${data.count === 1 ? 'moment' : 'moments'} added.`);
      } else {
        await api.delete(`/api/connections/${name}`);
        setPending('');
        setNotice(`${name} disconnected. Its synced entries were removed.`);
      }
      await load();
      await refresh();
    } catch {
      setError(`Could not ${action} ${name}. Check the API and provider settings, then try again.`);
    } finally {
      setBusy('');
    }
  }
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">BRING YOUR WORLD TOGETHER</p>
          <h1>
            A few good connections<span>.</span>
          </h1>
          <p className="page-description">Give the different parts of your day a place to meet.</p>
        </div>
        <span className="small-badge">
          {isDemo ? 'Demo workspace' : `${connections.length} connected`}
        </span>
      </div>
      {isDemo && (
        <div className="demo-banner">
          <Icon name="Manual" />
          <div>
            <strong>Take a look around.</strong>
            <p>These are fictional sample connections. Your real accounts stay untouched.</p>
          </div>
          <Link to="/login" className="text-link">
            Use my accounts <Icon name="arrow" size={16} />
          </Link>
        </div>
      )}
      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className="error-banner" role="alert">
          {error}{' '}
          <button className="text-button" onClick={() => void load()}>
            Retry
          </button>
        </p>
      )}
      <div className="connection-grid">
        {providers.map((provider) => {
          const connection = connections.find((c) => c.apiProvider === provider.name);
          const connected = Boolean(connection);
          return (
            <article
              key={provider.name}
              className={`connection-card source-${provider.name.toLowerCase()}`}
            >
              <div className="connection-top">
                <span className="source-icon">
                  <Icon name={provider.name} size={27} />
                </span>
                <span className={`connection-status ${connected ? 'connected' : ''}`}>
                  {isDemo
                    ? 'Sample data'
                    : loading
                      ? 'Loading…'
                      : connected
                        ? 'Connected'
                        : 'Not connected'}
                </span>
              </div>
              <h2>{provider.name}</h2>
              <h3>{provider.title}</h3>
              <p>{provider.description}</p>
              <div className="connection-detail">
                {isDemo
                  ? `${entries.filter((e) => e.sourceApi === provider.name).length} sample moments`
                  : connection
                    ? `Last synced ${new Date(connection.lastSyncAt).toLocaleString()}`
                    : 'Connect when you are ready.'}
              </div>
              <button
                className="button secondary full-width"
                disabled={Boolean(busy) || loading}
                onClick={() => void act(provider.name, connected ? 'sync' : 'connect')}
              >
                {busy === provider.name
                  ? 'Working…'
                  : isDemo
                    ? 'Explore connection'
                    : connected
                      ? 'Sync now'
                      : 'Connect account'}
                <Icon name={connected ? 'refresh' : 'arrow'} size={17} />
              </button>
              {connected && (
                <button
                  className="text-button disconnect-button"
                  disabled={Boolean(busy)}
                  onClick={() => setPending(provider.name)}
                >
                  Disconnect
                </button>
              )}
              {pending === provider.name && (
                <div
                  className="disconnect-confirm"
                  role="group"
                  aria-label={`Confirm disconnect ${provider.name}`}
                >
                  <p>
                    Disconnecting removes all {provider.name} activity from this timeline. Manual
                    moments stay.
                  </p>
                  <button
                    className="button danger"
                    disabled={Boolean(busy)}
                    onClick={() => void act(provider.name, 'disconnect')}
                  >
                    Disconnect and remove activity
                  </button>
                  <button
                    className="text-button"
                    disabled={Boolean(busy)}
                    onClick={() => setPending('')}
                  >
                    Cancel
                  </button>
                </div>
              )}
            </article>
          );
        })}
      </div>
      <section className="connection-explainer panel">
        <span className="source-icon">
          <Icon name="refresh" />
        </span>
        <div>
          <h2>A timeline that grows with you.</h2>
          <p>
            Connected accounts sync on demand and through the API’s hourly background worker.
            Provider history windows and API permissions determine what can be imported. Your own
            notes always have a place alongside them.
          </p>
          <Link className="text-link" to="/timeline?new=1">
            Capture something yourself <Icon name="arrow" size={16} />
          </Link>
        </div>
      </section>
      {isDemo && (
        <section className="demo-reset">
          <div>
            <h3>Ready for a fresh look?</h3>
            <p>Reset removes your demo edits and restores fictional sample moments.</p>
          </div>
          <button className="button secondary" onClick={() => setPending('demo')}>
            Reset demo
          </button>
          {pending === 'demo' && (
            <div className="reset-confirm">
              <span>Replace all demo changes?</span>
              <button
                className="button danger"
                onClick={() => {
                  try {
                    resetDemo();
                    setPending('');
                    setNotice('Demo restored. A fresh week of sample moments is ready.');
                  } catch {
                    setError('Could not reset the demo. Check your browser storage.');
                  }
                }}
              >
                Yes, reset demo
              </button>
              <button className="text-button" onClick={() => setPending('')}>
                Cancel
              </button>
            </div>
          )}
        </section>
      )}
    </>
  );
}
