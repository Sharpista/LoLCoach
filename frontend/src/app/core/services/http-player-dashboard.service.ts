import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { apiBaseUrl } from '../config/api-config';
import { PlayerDashboard } from '../models/dashboard';
import { DashboardError, PlayerDashboardService } from './player-dashboard.service';

/** Provider HTTP do dashboard — sync antes da análise (spec 002/003/004). */
@Injectable()
export class HttpPlayerDashboardService extends PlayerDashboardService {
  private readonly http = inject(HttpClient);

  override syncMatches(playerId: string): Observable<unknown> {
    return this.http
      .post<unknown>(
        `${apiBaseUrl()}/api/players/${encodeURIComponent(playerId)}/matches/sync`,
        null,
      )
      .pipe(catchError((err: unknown) => this.mapError(err, 'Falha ao sincronizar partidas.')));
  }

  override getDashboard(playerId: string): Observable<PlayerDashboard> {
    return this.http
      .get<PlayerDashboard>(`${apiBaseUrl()}/api/players/${encodeURIComponent(playerId)}/analysis`)
      .pipe(catchError((err: unknown) => this.mapError(err, 'Falha ao carregar análise.')));
  }

  private mapError(err: unknown, fallback: string): Observable<never> {
    const http = err as HttpErrorResponse;
    const status = typeof http?.status === 'number' ? http.status : 500;
    const message =
      status === 404
        ? 'Jogador não encontrado.'
        : status === 401
          ? 'Sua sessão expirou. Entre novamente para sincronizar partidas.'
          : status === 503
            ? 'O serviço da Riot está indisponível. Tente novamente em alguns instantes.'
            : status === 429
              ? 'A Riot limitou as consultas. Tente novamente em alguns instantes.'
              : fallback;
    return throwError(() => new DashboardError(message, status));
  }
}
