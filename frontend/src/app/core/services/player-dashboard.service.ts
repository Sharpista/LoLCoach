import { Observable } from 'rxjs';
import { PlayerDashboard } from '../models/dashboard';

/**
 * Contrato do dashboard (spec 004).
 *
 * O backend 002/003 ainda não existe. O provider atual é o
 * `MockPlayerDashboardService`. Quando a API real for implementada
 * (ver `specs/004-dashboard/design.md` — GET /api/players/{id}/analysis),
 * criar um `HttpPlayerDashboardService` que estende esta classe e trocar o
 * provider em `app.config.ts` via `useClass`. A UI e o contrato de dados
 * não mudam.
 */
export abstract class PlayerDashboardService {
  abstract getDashboard(playerId: string): Observable<PlayerDashboard>;
}

/**
 * Erro tipado do dashboard. O provider HTTP futuro mapeia `HttpErrorResponse`
 * para este tipo preservando o `status` (404 => jogador não encontrado).
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
