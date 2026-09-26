import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PlayerDashboard } from '../models/dashboard';
import { DashboardError } from './player-dashboard.service';
import { HttpPlayerDashboardService } from './http-player-dashboard.service';

const sample: PlayerDashboard = {
  player: {
    id: '11111111-1111-1111-1111-111111111111',
    gameName: 'Faker',
    tagLine: 'KR1',
    region: 'kr',
  },
  summary: {
    matchesAnalysed: 3,
    winrate: 33.33,
    kda: 1.2,
    csPerMin: 5,
    visionPerMin: 0.5,
    damagePerMin: 400,
  },
  insights: [
    { id: 'high_deaths', severity: 'high', title: 'Mortes excessivas', description: '...' },
  ],
  champions: [{ champion: 'Ahri', games: 1, winrate: 0, kda: 0.5 }],
  recentMatches: [
    {
      id: '22222222-2222-2222-2222-222222222222',
      champion: 'Lux',
      result: 'loss',
      kda: '2/9/5',
      cs: 100,
      durationMinutes: 30,
      playedAt: '2026-09-03T00:00:00Z',
    },
  ],
};

describe('HttpPlayerDashboardService', () => {
  let service: HttpPlayerDashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HttpPlayerDashboardService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HttpPlayerDashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('GET /api/players/{id}/analysis', () => {
    let result: PlayerDashboard | undefined;
    service.getDashboard(sample.player.id).subscribe((r) => (result = r));

    const req = httpMock.expectOne(
      `http://localhost:5181/api/players/${sample.player.id}/analysis`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(sample);
    expect(result).toEqual(sample);
  });

  it('mapeia 404 para DashboardError', () => {
    let err: DashboardError | undefined;
    service.getDashboard(sample.player.id).subscribe({
      error: (e: DashboardError) => (err = e),
    });

    httpMock
      .expectOne(`http://localhost:5181/api/players/${sample.player.id}/analysis`)
      .flush({ title: 'Player not found' }, { status: 404, statusText: 'Not Found' });

    expect(err).toBeInstanceOf(DashboardError);
    expect(err?.status).toBe(404);
  });

  it('POST /api/players/{id}/matches/sync', () => {
    service.syncMatches(sample.player.id).subscribe();

    const req = httpMock.expectOne(
      `http://localhost:5181/api/players/${sample.player.id}/matches/sync`,
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({ imported: 1, skipped: 0, failed: 0 });
  });

  it('mapeia 503 do sync para erro acionável', () => {
    let err: DashboardError | undefined;
    service.syncMatches(sample.player.id).subscribe({ error: (e: DashboardError) => (err = e) });

    httpMock
      .expectOne(`http://localhost:5181/api/players/${sample.player.id}/matches/sync`)
      .flush('unavailable', { status: 503, statusText: 'Service Unavailable' });

    expect(err?.status).toBe(503);
    expect(err?.message).toContain('Riot');
  });

  it('mapeia 500 para DashboardError', () => {
    let err: DashboardError | undefined;
    service.getDashboard(sample.player.id).subscribe({
      error: (e: DashboardError) => (err = e),
    });

    httpMock
      .expectOne(`http://localhost:5181/api/players/${sample.player.id}/analysis`)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    expect(err).toBeInstanceOf(DashboardError);
    expect(err?.status).toBe(500);
  });
});
