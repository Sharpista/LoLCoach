import { Observable } from 'rxjs';
import { PlayerDashboard } from '../models/dashboard';

/**
 * Contrato do dashboard (spec 004).
 * Provider real: `HttpPlayerDashboardService` → GET /api/players/{id}/analysis.
 * Mock permanece em `mock-player-dashboard.service.ts` para demos manuais.
 */
export abstract class PlayerDashboardService {
  abstract getDashboard(playerId: string): Observable<PlayerDashboard>;
}

/**
 * Erro tipado do dashboard. HTTP mapeia `HttpErrorResponse` preservando
 * `status` (404 => jogador não encontrado).
 */
export class DashboardError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = 'DashboardError';
  }
}
