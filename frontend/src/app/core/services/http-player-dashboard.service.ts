import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { apiBaseUrl } from '../config/api-config';
import { PlayerDashboard } from '../models/dashboard';
import { DashboardError, PlayerDashboardService } from './player-dashboard.service';

/** Provider HTTP do dashboard — GET /api/players/{id}/analysis (spec 003/004). */
@Injectable()
export class HttpPlayerDashboardService extends PlayerDashboardService {
  private readonly http = inject(HttpClient);

  override getDashboard(playerId: string): Observable<PlayerDashboard> {
    return this.http
      .get<PlayerDashboard>(`${apiBaseUrl()}/api/players/${encodeURIComponent(playerId)}/analysis`)
      .pipe(
        catchError((err: unknown) => {
          const http = err as HttpErrorResponse;
          const status = typeof http?.status === 'number' ? http.status : 500;
          const message =
            status === 404 ? 'Jogador não encontrado.' : 'Falha ao carregar análise.';
          return throwError(() => new DashboardError(message, status));
        }),
      );
  }
}
