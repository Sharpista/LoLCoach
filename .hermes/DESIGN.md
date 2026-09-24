# LoLCoach Design System

Este arquivo é a referência visual do frontend. Mudanças de interface devem consultar este documento e `specs/004-dashboard/design.md` antes da implementação.

## Direção visual

- Produto: coach de desempenho para jogadores de League of Legends.
- Prioridade: leitura rápida, comparação objetiva e próximos passos acionáveis.
- Tom: técnico, direto e calmo; o produto diagnostica sem punir o jogador.
- Densidade: dados importantes aparecem primeiro; detalhes ficam abaixo do resumo.

## Tokens

- Neutros: Tailwind `stone`; fundo claro `#faf8f5`, texto `stone-900`.
- Tema escuro: fundo `stone-950` (`#0c0a09`), texto `stone-100`.
- Acento: `amber` para foco, seleção e estados de destaque.
- Tipografia: `Syne` para títulos, `Plus Jakarta Sans` para corpo, `JetBrains Mono` para dados e rótulos técnicos.
- Tema escuro: variante Tailwind `dark` por classe no elemento `html`, controlada pelo `ThemeService`.
- Ícones: SVG inline ou `lucide-angular`; não usar emoji como ícone de interface.

## Composição

1. Resumo de desempenho.
2. Três problemas principais em destaque.
3. Performance por campeão.
4. Partidas recentes.
5. Recomendações e coach report quando disponíveis.

Estados obrigatórios de uma tela de dados: `loading`, `ready`, `no-matches`, `not-found` e `error`. Cada estado precisa de mensagem clara e uma ação possível quando houver recuperação.

## Acessibilidade e interação

- Usar HTML semântico, ordem de foco previsível e contraste suficiente nos dois temas.
- Todo controle interativo deve ter nome acessível, estado de foco visível e alvo adequado para toque.
- Animações devem ser curtas, informar mudança de estado e respeitar `prefers-reduced-motion`.
- Dados tabulares e métricas devem continuar compreensíveis sem cor isoladamente.

## Implementação

- Stack visual: Angular 22 + Tailwind CSS v4; não introduzir React, Vite ou outra biblioteca de UI sem decisão registrada em uma spec.
- Estilos globais: `frontend/src/styles.css`.
- Componentes do dashboard: `frontend/src/app/features/player-dashboard/`.
- Busca de jogador: `frontend/src/app/features/player-search/`.
- Antes de criar uma tela, preencher o brief visual na `design.md` da feature e validar o resultado com a checklist de acessibilidade.
