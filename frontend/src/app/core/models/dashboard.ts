/** Dados de identidade do jogador no dashboard. */
export interface PlayerSummary {
  id: string;
  gameName: string;
  tagLine: string;
  region: string;
}

/** Resumo de performance (spec 004). */
export interface PerformanceSummary {
  /** Total de partidas analisadas. */
  matchesAnalysed: number;
  /** 0–100. */
  winrate: number;
  kda: number;
  csPerMin: number;
  visionPerMin: number;
  damagePerMin: number;
}

export type InsightSeverity = 'high' | 'medium' | 'low';

export interface Insight {
  id: string;
  severity: InsightSeverity;
  title: string;
  description: string;
  /** Campos extras do DTO 003 — opcionais na UI. */
  type?: string;
  metric?: string;
  currentValue?: number;
  targetValue?: number;
  matchesAnalyzed?: number;
}

export interface ChampionPerformance {
  champion: string;
  games: number;
  /** 0–100. */
  winrate: number;
  kda: number;
}

export interface RecentMatch {
  id: string;
  champion: string;
  result: 'win' | 'loss';
  /** Ex.: "9/2/7". */
  kda: string;
  cs: number;
  durationMinutes: number;
  /** ISO 8601 UTC. */
  playedAt: string;
}

/** Contrato do dashboard (spec 004). */
export interface PlayerDashboard {
  player: PlayerSummary;
  summary: PerformanceSummary;
  /** Top 3 insights. */
  insights: Insight[];
  champions: ChampionPerformance[];
  recentMatches: RecentMatch[];
}
