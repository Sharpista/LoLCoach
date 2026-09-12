import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PlayerDashboard as PlayerDashboardData } from '../../core/models/dashboard';
import { DashboardError, PlayerDashboardService } from '../../core/services/player-dashboard.service';
import { PlayerDashboard } from './player-dashboard';

function mockDashboard(): PlayerDashboardData {
  return {
    player: { id: 'abc', gameName: 'Faker', tagLine: 'KR1', region: 'kr' },
    summary: {
      matchesAnalysed: 30,
      winrate: 56.7,
      kda: 3.2,
      csPerMin: 8.1,
      visionPerMin: 0.9,
      damagePerMin: 611,
    },
    insights: [
      { id: 'i1', severity: 'high', title: 'Visão', description: 'desc' },
      { id: 'i2', severity: 'medium', title: 'CS', description: 'desc' },
      { id: 'i3', severity: 'low', title: 'KDA', description: 'desc' },
    ],
    champions: [{ champion: 'Ahri', games: 8, winrate: 62.5, kda: 4.1 }],
    recentMatches: [
      { id: 'm1', champion: 'Ahri', result: 'win', kda: '9/2/7', cs: 214, durationMinutes: 28, playedAt: '2026-09-11T20:00:00Z' },
    ],
  };
}

describe('PlayerDashboard', () => {
  let routeParams: { id?: string };
  let serviceSpy: { getDashboard: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    routeParams = { id: 'abc' };
    serviceSpy = { getDashboard: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PlayerDashboard],
      providers: [
        provideRouter([]),
        { provide: PlayerDashboardService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) =>
                  (routeParams as Record<string, string | undefined>)[key] ?? null,
              },
            },
          },
        },
      ],
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(PlayerDashboard);
    fixture.detectChanges(); // dispara ngOnInit
    return { fixture, component: fixture.componentInstance };
  }

  it('deve exibir loading enquanto carrega', () => {
    const subject = new Subject<PlayerDashboardData>();
    serviceSpy.getDashboard.mockReturnValue(subject.asObservable());
    const { component } = createComponent();
    expect(component.status()).toBe('loading');
    subject.next(mockDashboard());
    subject.complete();
    expect(component.status()).toBe('ready');
  });

  it('deve renderizar as seções no estado ready', () => {
    serviceSpy.getDashboard.mockReturnValue(of(mockDashboard()));
    const { fixture, component } = createComponent();
    expect(component.status()).toBe('ready');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-performance-summary')).toBeTruthy();
    expect(el.querySelector('app-insights-summary')).toBeTruthy();
    expect(el.querySelector('app-champion-performance')).toBeTruthy();
    expect(el.querySelector('app-recent-matches')).toBeTruthy();
  });

  it('deve tratar jogador não encontrado (404)', () => {
    serviceSpy.getDashboard.mockReturnValue(
      throwError(() => new DashboardError('não encontrado', 404)),
    );
    const { component } = createComponent();
    expect(component.status()).toBe('not-found');
  });

  it('deve tratar estado sem partidas', () => {
    const empty = mockDashboard();
    empty.summary.matchesAnalysed = 0;
    empty.insights = [];
    empty.champions = [];
    empty.recentMatches = [];
    serviceSpy.getDashboard.mockReturnValue(of(empty));
    const { component } = createComponent();
    expect(component.status()).toBe('no-matches');
  });

  it('deve tratar erro externo', () => {
    serviceSpy.getDashboard.mockReturnValue(
      throwError(() => new DashboardError('falha', 500)),
    );
    const { component } = createComponent();
    expect(component.status()).toBe('error');
  });
});
