/**
 * AI Coach — spec 006 — NÃO IMPLEMENTADO (stub documentado).
 *
 * O AI Coach (Gemini) será consumido EXCLUSIVAMENTE via BACKEND.
 * O navegador NUNCA deve chamar a API Gemini diretamente:
 *   - a chave (GEMINI_API_KEY) vive apenas no backend — nunca no bundle do
 *     client nem em environment (o placeholder `environment.geminiApiKey`
 *     existe só como referência de configuração do backend, e permanece vazio);
 *   - o frontend apenas consome o relatório estruturado pronto (`CoachReport`).
 *
 * Quando a spec 006 for implementada:
 *   1. criar um endpoint no backend (ex.: GET/POST `/api/players/{id}/coach`)
 *      que orquestra o Gemini no servidor;
 *   2. descomentar e adaptar o serviço abaixo para fazer apenas esse HTTP.
 * Nenhuma chamada a `generativelanguage.googleapis.com` deve existir no client.
 */

// import { HttpClient } from '@angular/common/http';
// import { Injectable, inject } from '@angular/core';
// import { Observable } from 'rxjs';
// import { apiBaseUrl } from '../config/api-config';

/** Relatório estruturado devolvido pelo backend (contrato futuro). */
export interface CoachReport {
  periodSummary: string;
  strengths: string[];
  weaknesses: string[];
  topPriority: string;
  goals: string[];
}

/*
@Injectable({ providedIn: 'root' })
export class AiCoachService {
  private readonly http = inject(HttpClient);

  getCoachReport(playerId: string): Observable<CoachReport> {
    // Consome o backend — nunca o Gemini diretamente.
    return this.http.get<CoachReport>(`${apiBaseUrl()}/api/players/${playerId}/coach`);
  }
}
*/
