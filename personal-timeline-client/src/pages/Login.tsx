import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { API_URL } from '../lib/api';
import { Brand, Icon } from '../components/Icon';
export function Login() {
  const { startDemo } = useAuth();
  const navigate = useNavigate();
  return (
    <div className="login-page">
      <header>
        <Link to="/login" aria-label="Dayweave home">
          <Brand />
        </Link>
        <span>A personal activity journal</span>
      </header>
      <main className="login-grid">
        <section>
          <p className="eyebrow">A LITTLE MORE CONNECTED</p>
          <h1>
            Your digital life.
            <br />
            <em>With a little more life.</em>
          </h1>
          <p className="login-description">
            The code you ship. The songs you repeat. The moments you want to keep. Weave them into a
            story that feels like you.
          </p>
          <div className="login-actions">
            <button
              className="button primary"
              onClick={() => {
                startDemo();
                navigate('/');
              }}
            >
              Explore the demo <Icon name="arrow" size={18} />
            </button>
            <a className="button secondary" href={`${API_URL}/login/github`}>
              <Icon name="GitHub" size={18} /> Sign in with GitHub
            </a>
          </div>
          <p className="login-caption">No sign-up needed for the demo. Just a little curiosity.</p>
          <div className="login-sources">
            MADE FOR YOUR EVERYDAY{' '}
            <span>
              <Icon name="GitHub" /> GitHub
            </span>
            <span>
              <Icon name="Spotify" /> Spotify
            </span>
            <span>
              <Icon name="YouTube" /> YouTube
            </span>
          </div>
        </section>
        <section className="login-story" aria-label="Example timeline">
          <span className="eyebrow">AN ORDINARY DAY, BEAUTIFULLY KEPT</span>
          <h2>It all belongs here.</h2>
          <div className="story-line">
            {[
              [
                'GitHub',
                '10:15 AM',
                'Made something a little better',
                'One commit closer to an idea.',
              ],
              [
                'Spotify',
                '12:30 PM',
                'Found a song worth repeating',
                'A soundtrack for the in-between.',
              ],
              ['Manual', '6:45 PM', 'Took the long way home', 'And remembered to look up.'],
            ].map(([source, time, title, description]) => (
              <article className={`story-item source-${source.toLowerCase()}`} key={source}>
                <span className="source-icon">
                  <Icon name={source} />
                </span>
                <div>
                  <small>{time}</small>
                  <h3>{title}</h3>
                  <p>{description}</p>
                </div>
              </article>
            ))}
          </div>
          <div className="story-foot">
            Small moments. A bigger picture. <span>✳</span>
          </div>
        </section>
      </main>
      <footer>Dayweave · Your days, connected.</footer>
    </div>
  );
}
export function OAuthCallback() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const handled = useRef(false);
  const [error, setError] = useState(false);
  useEffect(() => {
    if (handled.current) return;
    handled.current = true;
    const token = new URLSearchParams(window.location.hash.slice(1)).get('token');
    window.history.replaceState(null, '', window.location.pathname + window.location.search);
    if (token && login(token)) navigate('/', { replace: true });
    else setError(true);
  }, [login, navigate]);
  return (
    <main className="auth-state">
      {error ? (
        <>
          <h1>Let’s try that again.</h1>
          <p>The sign-in session is missing or has expired.</p>
          <Link to="/login" className="button primary">
            Back to sign in
          </Link>
        </>
      ) : (
        <p role="status">Bringing your workspace together…</p>
      )}
    </main>
  );
}
