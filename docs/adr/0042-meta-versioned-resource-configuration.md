# 0042 — Catálogo versionado e edição parcial de recursos Meta

## Status

Implementado em 2026-10-22.

## Contexto

O formulário anterior expunha poucos parâmetros e não carregava todas as configurações atuais. A API possui operações e estruturas diferentes por recurso; formulários com padrões locais podem alterar configurações que o usuário não pretendia modificar.

## Decisão

Usar um catálogo incorporado derivado por análise AST do SDK oficial Meta v26.0/26.0.2, com commit fixado, operações, tipos, enums e subestruturas. O backend publica opções somente para conta autorizada. A leitura verifica propriedade e retorna campos indisponíveis separadamente; a Graph é a autoridade de elegibilidade. O editor Blazor consome os contratos HTTP, distingue criação/edição e envia somente campos selecionados. Objetos mantêm propriedades existentes. Estruturas especializadas podem ser editadas em JSON validado. O diário idempotente, criação pausada, confirmação de ativação e verificação de propriedade de alvo/pais/ativos/origens continuam obrigatórios. Não há proxy Graph genérico ou credenciais no payload.

## Consequências

Mais opções podem ser expostas sem duplicar centenas de formulários; validação e apresentação compartilham a mesma referência versionada. A versão precisa ser atualizada deliberadamente e o esquema não garante elegibilidade de uma conta. Leituras adicionais podem exigir chamadas por campo, limitadas a quatro simultâneas quando uma consulta agregada falha. Experimentos completos e configurações administrativas da Meta continuam em fluxos separados. Testes cobrem payload parcial, isolamento de conta, valores falsos/zero, segmentação preservada e confirmação de ativação.
