import { Component, ErrorInfo, ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { TimelineProvider } from './context/TimelineContext';
import Shell from './components/Shell';
import Overview from './pages/Overview';
import Timeline from './pages/Timeline';
import Connections from './pages/Connections';
import { Login, OAuthCallback } from './pages/Login';
class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  componentDidCatch(_error: Error, _info: ErrorInfo) {
    /* Avoid logging personal timeline data. */
  }
  render() {
    return this.state.failed ? (
      <main className="auth-state">
        <h1>Something interrupted your day.</h1>
        <p>Your saved moments are still in your browser or account.</p>
        <button className="button primary" onClick={() => window.location.reload()}>
          Try again
        </button>
      </main>
    ) : (
      this.props.children
    );
  }
}
function Workspace() {
  const { user } = useAuth();
  return user ? <Shell /> : <Navigate to="/login" replace />;
}
export default function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter basename={import.meta.env.BASE_URL}>
        <AuthProvider>
          <TimelineProvider>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route path="/oauth-callback" element={<OAuthCallback />} />
              <Route element={<Workspace />}>
                <Route index element={<Overview />} />
                <Route path="/timeline" element={<Timeline />} />
                <Route path="/connections" element={<Connections />} />
              </Route>
              <Route path="/settings" element={<Navigate to="/connections" replace />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </TimelineProvider>
        </AuthProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}
