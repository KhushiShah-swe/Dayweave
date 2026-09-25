import { jwtDecode } from 'jwt-decode';
import { User } from '../types';
export const TOKEN_KEY = 'dayweave.session';
export const MODE_KEY = 'dayweave.mode';
export function userFromToken(token: string | null): User | null {
  if (!token) return null;
  try {
    const value = jwtDecode<Record<string, unknown>>(token);
    const id = Number(
      value['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? value.sub,
    );
    if (
      typeof value.exp !== 'number' ||
      value.exp * 1000 <= Date.now() ||
      !Number.isInteger(id) ||
      id < 1
    )
      return null;
    return {
      id,
      displayName: String(
        value['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
          value.name ??
          'Your account',
      ),
    };
  } catch {
    return null;
  }
}
export function getStored(key: string) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}
export function setStored(key: string, value: string | null) {
  try {
    value === null ? localStorage.removeItem(key) : localStorage.setItem(key, value);
  } catch {
    /* Browsing remains usable if storage is blocked. */
  }
}
