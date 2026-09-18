# Especificações do LoLCoach

Cada mudança de produto deve ter uma pasta em `specs/{id}-{nome}` com `spec.md`, `design.md` e `tasks.md`. Esses arquivos são o contrato entre descoberta, implementação e revisão.

## Ciclo de trabalho

1. **Descoberta** — esclarecer problema, usuário, resultado esperado e restrições. Registrar suposições e perguntas abertas.
2. **PRD/spec** — converter o problema em requisitos observáveis, critérios de aceitação, fora de escopo e riscos.
3. **Red team** — procurar ambiguidades, métricas não definidas, falhas de segurança, acessibilidade e operação antes de marcar `READY`.
4. **Design** — registrar fluxo, estados, contrato de dados e, para frontend, o brief visual compatível com `.hermes/DESIGN.md`.
5. **Tasks** — decompor em unidades executáveis e associar cada uma a cenários de teste.
6. **Implementação** — trabalhar somente em tasks atribuídas e atualizar evidências à medida que cada task avança.
7. **Verificação** — executar build, testes, análise estática e revisão de segurança aplicáveis; registrar comando e resultado em `evidence/`.
8. **Review e encerramento** — revisar o diff, conferir critérios e promover o status para `DONE` apenas com evidência reproduzível.

## Estados

`DRAFT` → `READY` → `IN_PROGRESS` → `REVIEW` → `DONE`; use `BLOCKED` quando uma pergunta, dependência ou falha impedir o avanço.

## Evidência mínima

Uma feature concluída deve apontar para:

- comandos de build e testes executados;
- resultado e data da verificação;
- cenários manuais ou de navegador quando a mudança for visual;
- riscos ou limitações conhecidos;
- parecer de revisão, sem alegar testes que não foram executados.

Use `Open Questions` para decisões ainda abertas. Uma pergunta que altere comportamento, arquitetura ou segurança bloqueia a implementação.
