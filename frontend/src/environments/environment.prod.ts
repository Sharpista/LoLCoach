/**
 * Configuração de ambiente (produção).
 *
 * Em produção o frontend deve ser servido atrás do mesmo domínio/reverse-proxy
 * do backend, portanto `apiUrl` é vazio (caminhos relativos `/api/...`).
 * Alternativamente, injete `window.LOLCOACH_API_URL` no deploy para apontar a
 * um backend em outro domínio.
 */
export const environment = {
  production: true,
  apiUrl: '',
  geminiApiKey: '',
};
