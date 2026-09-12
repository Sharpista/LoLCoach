/** Corpo do POST /api/players/search (spec 001). */
export interface SearchPlayerRequest {
  gameName: string;
  tagLine: string;
  region: string;
}

/** Jogador retornado pelo backend (DTO camelCase). */
export interface Player {
  id: string;
  puuid: string;
  gameName: string;
  tagLine: string;
  region: string;
  createdAt: string;
  lastUpdatedAt: string;
}

/** Problem Details (application/problem+json) do backend. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  /** Validação 400: chaves camelCase -> array de mensagens. */
  errors?: Record<string, string[]>;
}
