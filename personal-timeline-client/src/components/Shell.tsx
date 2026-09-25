import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Brand, Icon } from './Icon';
export default function Shell() {
  const { user, isDemo, logout } = useAuth();
  const { pathname } = useLocation();
  const page =
    pathname === '/timeline'
      ? 'Timeline'
      : pathname === '/connections'
        ? 'Connections'
        : 'Overview';
  return (
    <div className="app-shell">
      <a href="#main-content" className="skip-link">
        Skip to content
      </a>
      <aside className="sidebar">
        <Link to="/" aria-label="Dayweave home">
          <Brand />
        </Link>
        <p className="sidebar-label">YOUR SPACE</p>
        <nav aria-label="Main navigation">
          {[
            ['/', 'grid', 'Overview'],
            ['/timeline', 'timeline', 'Timeline'],
            ['/connections', 'link', 'Connections'],
          ].map(([to, icon, label]) => (
            <NavLink to={to} end={to === '/'} key={to}>
              <Icon name={icon} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-note">
          <span className="small-spark">✳</span>
          <h3>
            A life is made
            <br />
            of little moments.
          </h3>
          <p>
            Keep the ones that
            <br />
            make it yours.
          </p>
          <Link to="/timeline?new=1">
            Capture a moment <Icon name="arrow" size={16} />
          </Link>
        </div>
        <div className="sidebar-account">
          <span className="avatar">
            {isDemo ? 'E' : user?.displayName.slice(0, 1).toUpperCase()}
          </span>
          <div>
            <strong>{user?.displayName}</strong>
            <small>{isDemo ? 'Demo workspace' : 'Personal workspace'}</small>
          </div>
          <button
            className="icon-button"
            onClick={logout}
            aria-label={isDemo ? 'Exit demo' : 'Sign out'}
          >
            <Icon name="exit" />
          </button>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div>
            <span className="muted">My workspace</span>
            <span className="slash">/</span>
            <strong>{page}</strong>
          </div>
          <div className="topbar-right">
            <span className="workspace-status">
              <i />
              {isDemo ? 'Interactive demo' : 'Your private timeline'}
            </span>
            <span className="avatar small">
              {isDemo ? 'E' : user?.displayName.slice(0, 1).toUpperCase()}
            </span>
          </div>
        </header>
        <main id="main-content" className="main-content">
          <Outlet />
        </main>
        <footer className="app-footer">
          <span>Dayweave · A little more connected.</span>
          <span>
            {isDemo
              ? 'Fictional sample activity · Saved in this browser'
              : 'Your activity, at your own pace.'}
          </span>
        </footer>
      </div>
    </div>
  );
}
