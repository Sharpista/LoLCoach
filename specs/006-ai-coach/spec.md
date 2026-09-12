---
id: 006
name: ai-coach
status: READY
depends_on:
  - 003
  - 005
---

# Spec 006 - AI Coach

## Objetivo

Usar um LLM somente para transformar métricas, insights e recomendações já calculados em uma explicação natural e personalizada.

## Entrada

- resumo de métricas
- principais insights
- recomendações

## Saída

- resumo do período
- até 3 pontos fortes
- até 3 pontos fracos
- prioridade principal
- plano de até 3 objetivos

## Restrições

A IA não deve:

- analisar JSON bruto da Riot como fonte primária;
- inventar estatísticas;
- alterar números calculados;
- afirmar algo sem evidência fornecida;
- criar métricas inexistentes.

## Critérios de aceitação

- Todo número apresentado no texto deve existir no payload estruturado.
- Falha do LLM não deve impedir o dashboard determinístico de funcionar.

## Decisões

- LLM do AI Coach: **Gemini 3.8** (provedor decidido pelo produto). O identificador exato do modelo (ex.: `gemini-3.8-*`) deve ser confirmado na implementação sem alterar o contrato de domínio.

## Open Questions

Nenhuma bloqueante. O identificador exato do modelo Gemini 3.8 pode ser definido na implementação.
