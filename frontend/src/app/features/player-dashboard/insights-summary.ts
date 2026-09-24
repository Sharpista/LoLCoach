import { Component, computed, input } from '@angular/core';
import { Insight, InsightSeverity } from '../../core/models/dashboard';

function severityLabel(severity: InsightSeverity): string {
  switch (severity) {
    case 'high':
      return 'Alta prioridade';
    case 'medium':
      return 'Média prioridade';
    default:
      return 'Baixa prioridade';
  }
}

/** Top 3 insights do jogador. */
@Component({
  selector: 'app-insights-summary',
  templateUrl: './insights-summary.html',
})
export class InsightsSummary {
  readonly insights = input.required<Insight[]>();

  readonly items = computed(() =>
    this.insights()
      .slice(0, 3)
      .map((i) => ({ ...i, label: severityLabel(i.severity) })),
  );
}
