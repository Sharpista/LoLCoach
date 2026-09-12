import { UpperCasePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PlayerDashboard as PlayerDashboardData } from '../../core/models/dashboard';
import { PlayerDashboardService } from '../../core/services/player-dashboard.service';
import { ChampionPerformance } from './champion-performance';
import { InsightsSummary } from './insights-summary';
import { PerformanceSummary } from './performance-summary';
import { RecentMatches } from './recent-matches';

type DashboardStatus = 'loading' | 'not-found' | 'no-matches' | 'error' | 'ready';

/** Dashboard do jogador (spec 004) — rota /player/:id. */
@Component({
  selector: 'app-player-dashboard',
  imports: [
    RouterLink,
    UpperCasePipe,
    PerformanceSummary,
    InsightsSummary,
    ChampionPerformance,
    RecentMatches,
  ],
  templateUrl: './player-dashboard.html',
  styleUrl: './player-dashboard.scss',
})
export class PlayerDashboard implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(PlayerDashboardService);

  readonly status = signal<DashboardStatus>('loading');
  readonly dashboard = signal<PlayerDashboardData | null>(null);
  readonly playerId = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.playerId.set(id);

    if (!id) {
      this.status.set('not-found');
      return;
    }

    this.service.getDashboard(id).subscribe({
      next: (d) => {
        this.dashboard.set(d);
        this.status.set(d.summary.matchesAnalysed === 0 ? 'no-matches' : 'ready');
      },
      error: (err: unknown) => {
        const status = (err as { status?: number })?.status;
        this.status.set(status === 404 ? 'not-found' : 'error');
      },
    });
  }
}
