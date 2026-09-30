import { Injectable } from '@angular/core';
import { delay, Observable, of, throwError } from 'rxjs';
import { PlayerDashboard } from '../models/dashboard';
import { DashboardError, PlayerDashboardService } from './player-dashboard.service';

/**
 * Provider MOCK do dashboard (spec 004). App usa HttpPlayerDashboardService;
 * mantenha este mock só para demos manuais (swap em app.config se precisar).
 *
 * IDs sentinela:
 *   - `/player/not-found` | `/player/no-matches` | `/player/error`
 */
@Injectable()
export class MockPlayerDashboardService extends PlayerDashboardService {
  override syncMatches(_playerId: string): Observable<unknown> {
    return of(null);
  }

  override getDashboard(playerId: string): Observable<PlayerDashboard> {
    if (playerId === 'not-found') {
      return throwError(() => new DashboardError('Jogador não encontrado.', 404));
    }
    if (playerId === 'error') {
      return throwError(() => new DashboardError('Falha ao carregar análise.', 500));
    }

    const dashboard =
      playerId === 'no-matches' ? emptyDashboard(playerId) : fullDashboard(playerId);
    return of(dashboard).pipe(delay(600));
  }
}

function fullDashboard(playerId: string): PlayerDashboard {
  return {
    player: { id: playerId, gameName: 'Faker', tagLine: 'KR1', region: 'kr' },
    summary: {
      matchesAnalysed: 30,
      winrate: 56.7,
      kda: 3.2,
      csPerMin: 8.1,
      visionPerMin: 0.9,
      damagePerMin: 611,
    },
    insights: [
      {
        id: 'i1',
        severity: 'high',
        title: 'Visão de mapa abaixo do esperado',
        description:
          'Seu vision/min (0.9) está ~18% abaixo da média da sua faixa. Priorize wards de controle antes dos objetivos.',
      },
      {
        id: 'i2',
        severity: 'medium',
        title: 'Queda de CS no meio do jogo',
        description:
          'Entre 15–25 min seu CS/min cai para 6.2. Recue ondas laterais após o recall para manter a renda.',
      },
      {
        id: 'i3',
        severity: 'low',
        title: 'Ótima participação em abates',
        description:
          'Seu KDA (3.2) está acima da média da faixa. Continue priorizando lutas por objetivos.',
      },
    ],
    champions: [
      { champion: 'Ahri', games: 8, winrate: 62.5, kda: 4.1 },
      { champion: 'Viktor', games: 6, winrate: 50.0, kda: 2.8 },
      { champion: 'Orianna', games: 5, winrate: 40.0, kda: 2.2 },
      { champion: 'Azir', games: 4, winrate: 75.0, kda: 3.9 },
      { champion: 'Sylas', games: 4, winrate: 50.0, kda: 2.5 },
    ],
    recentMatches: [
      { id: 'm1', champion: 'Ahri', result: 'win', kda: '9/2/7', cs: 214, durationMinutes: 28, playedAt: '2026-09-11T20:00:00Z' },
      { id: 'm2', champion: 'Viktor', result: 'loss', kda: '4/6/5', cs: 198, durationMinutes: 31, playedAt: '2026-09-11T18:30:00Z' },
      { id: 'm3', champion: 'Azir', result: 'win', kda: '7/1/9', cs: 236, durationMinutes: 26, playedAt: '2026-09-10T22:10:00Z' },
      { id: 'm4', champion: 'Orianna', result: 'loss', kda: '3/5/6', cs: 181, durationMinutes: 29, playedAt: '2026-09-10T19:45:00Z' },
      { id: 'm5', champion: 'Sylas', result: 'win', kda: '11/3/8', cs: 203, durationMinutes: 24, playedAt: '2026-09-09T21:20:00Z' },
    ],
  };
}

function emptyDashboard(playerId: string): PlayerDashboard {
  return {
    player: { id: playerId, gameName: 'SemPartidas', tagLine: 'BR1', region: 'br1' },
    summary: {
      matchesAnalysed: 0,
      winrate: 0,
      kda: 0,
      csPerMin: 0,
      visionPerMin: 0,
      damagePerMin: 0,
    },
    insights: [],
    champions: [],
    recentMatches: [],
  };
}
