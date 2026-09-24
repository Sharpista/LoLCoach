import { Component, computed, input } from '@angular/core';
import { PerformanceSummary as SummaryData } from '../../core/models/dashboard';

/** Resumo de performance: partidas, winrate, KDA, CS/min, vision/min, damage/min. */
@Component({
  selector: 'app-performance-summary',
  templateUrl: './performance-summary.html',
})
export class PerformanceSummary {
  readonly summary = input.required<SummaryData>();

  readonly stats = computed(() => {
    const s = this.summary();
    return [
      { label: 'Partidas analisadas', value: String(s.matchesAnalysed) },
      { label: 'Winrate', value: `${s.winrate.toFixed(1)}%` },
      { label: 'KDA', value: s.kda.toFixed(2) },
      { label: 'CS/min', value: s.csPerMin.toFixed(1) },
      { label: 'Vision/min', value: s.visionPerMin.toFixed(2) },
      { label: 'Damage/min', value: s.damagePerMin.toFixed(0) },
    ];
  });
}
