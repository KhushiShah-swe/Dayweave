import axios from 'axios';
import { getStored, TOKEN_KEY } from './session';
export const API_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5167').replace(/\/$/, '');
export const api = axios.create({ baseURL: API_URL, timeout: 30000 });
api.interceptors.request.use((config) => {
  const token = getStored(TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) window.dispatchEvent(new Event('dayweave:unauthorized'));
    return Promise.reject(error);
  },
);
