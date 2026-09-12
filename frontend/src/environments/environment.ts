/**
 * Configuração de ambiente (desenvolvimento).
 *
 * `apiUrl` é a base do backend LoLCoach. A porta 5181 é a documentada no
 * `backend/README.md` (`dotnet run --urls http://127.0.0.1:5181`).
 *
 * Override em runtime: defina `window.LOLCOACH_API_URL` antes do bootstrap
 * (ex.: via snippet no index de deploy) — tem precedência sobre este valor.
 * Override em build: use `ng build --configuration production` (troca para
 * `environment.prod.ts` via fileReplacements em angular.json).
 */
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5181',

  /**
   * AI Coach (spec 006) — NÃO IMPLEMENTADO.
   * A chave Gemini vive APENAS no backend (GEMINI_API_KEY). Nunca coloque a
   * chave aqui nem chame a API Gemini diretamente do browser — o frontend
   * consome somente o relatório estruturado via backend. Placeholder mantido
   * apenas como referência de configuração (ver .env.example na raiz).
   */
  geminiApiKey: '',
};
