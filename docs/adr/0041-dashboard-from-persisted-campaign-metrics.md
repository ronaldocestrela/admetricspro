# ADR 0041: Dashboard com métricas persistidas de campanhas

## Status
Implementado.

## Contexto
O dashboard gerava séries sintéticas a partir do workspace, ignorando importações reais. Analytics não pode consultar repositórios ou DbContexts de outros módulos diretamente.

## Decisão
Exportar `GetDashboardMetricPointsQuery` e `GetAnalyticsWorkspaceIdsQuery` em BuildingBlocks.Application e despachá-los pelo MediatR in-memory. Tenants autoriza a carteira com sessão e ViewCampaigns; Integrations consulta seu próprio repositório contextualizado pelo banco do tenant. Analytics calcula o dashboard com os registros Daily retornados e propaga falhas como Result. Não somar Hourly e Daily, inventar dispositivos ou aplicar cotações fixas a contas reais. Moeda diferente do filtro retorna Dashboard.CurrencyMismatch. O portal solicita a primeira importação de 60 dias após a seleção das contas Meta.

## Consequências
O painel reflete o que foi efetivamente persistido, incluindo dados demonstrativos apenas quando explicitamente provisionados no fluxo existente. Sem dados, retorna vazio. O acesso respeita tenant, carteira, workspace, período e moeda. Autorizar OAuth sem selecionar contas e concluir importação não produz métricas. Nenhuma nova persistência, fila ou referência direta de persistência entre módulos foi introduzida.
