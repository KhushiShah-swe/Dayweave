import { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { User } from '../types';
import { getStored, setStored, userFromToken, TOKEN_KEY, MODE_KEY } from '../lib/session';
interface Auth {
  user: User | null;
  isDemo: boolean;
  login: (token: string) => boolean;
  startDemo: () => void;
  logout: () => void;
}
const Context = createContext<Auth | null>(null);
export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getStored(TOKEN_KEY));
  const [isDemo, setDemo] = useState(() => getStored(MODE_KEY) === 'demo');
  const user = isDemo ? { id: 0, displayName: 'Explorer' } : userFromToken(token);
  function logout() {
    setToken(null);
    setDemo(false);
    setStored(TOKEN_KEY, null);
    setStored(MODE_KEY, null);
  }
  function login(next: string) {
    if (!userFromToken(next)) {
      logout();
      return false;
    }
    setStored(TOKEN_KEY, next);
    setStored(MODE_KEY, null);
    setToken(next);
    setDemo(false);
    return true;
  }
  function startDemo() {
    setStored(TOKEN_KEY, null);
    setStored(MODE_KEY, 'demo');
    setToken(null);
    setDemo(true);
  }
  useEffect(() => {
    const expire = () => {
      if (!isDemo) logout();
    };
    window.addEventListener('dayweave:unauthorized', expire);
    const timer = window.setInterval(() => {
      if (token && !userFromToken(token)) expire();
    }, 30000);
    return () => {
      window.removeEventListener('dayweave:unauthorized', expire);
      window.clearInterval(timer);
    };
  }, [token, isDemo]);
  return (
    <Context.Provider value={{ user, isDemo, login, startDemo, logout }}>
      {children}
    </Context.Provider>
  );
}
export function useAuth() {
  const value = useContext(Context);
  if (!value) throw new Error('AuthProvider is required');
  return value;
}
