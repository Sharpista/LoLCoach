import { Component, input } from '@angular/core';
import { ChampionPerformance as ChampionData } from '../../core/models/dashboard';

/** Desempenho por campeão. */
@Component({
  selector: 'app-champion-performance',
  templateUrl: './champion-performance.html',
  styleUrl: './champion-performance.scss',
})
export class ChampionPerformance {
  readonly champions = input.required<ChampionData[]>();
}
