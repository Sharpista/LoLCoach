import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    /** Override em runtime da base da API (precedência sobre environment.apiUrl). */
    LOLCOACH_API_URL?: string;
  }
}

/**
 * Base URL da API (sem barra final).
 *
 * Precedência:
 *   1. `window.LOLCOACH_API_URL` (override em runtime, útil para deploys);
 *   2. `environment.apiUrl` (dev: http://localhost:5181; prod: relativo).
 */
export function apiBaseUrl(): string {
  const override = typeof window !== 'undefined' ? window.LOLCOACH_API_URL : undefined;
  const raw = (override ?? '').trim() || environment.apiUrl;
  return raw.replace(/\/+$/, '');
}
