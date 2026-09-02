# ADR 0002: Database-per-Tenant for Provisioning

## Status

Accepted

## Context

A plataforma precisa de isolamento forte entre clientes (tenants), incluindo segregacao de dados, backup independente e menor risco de vazamento cruzado.

## Decision

Adotar estrategia database-per-tenant com catalogo central no MasterDb.

No fluxo de provisionamento da Subfase 1.2:

- O banco do tenant e criado dinamicamente no SQL Server.
- O schema operacional inicial do tenant e aplicado no banco dedicado.
- A connection string do tenant e armazenada criptografada no MasterDb.

## Consequences

Positivas:

- Isolamento forte por cliente.
- Recuperacao e manutencao por tenant simplificadas.
- Menor superficie de impacto em incidentes de dados.

Negativas:

- Maior custo operacional de migracoes em multiplos bancos.
- Necessidade de automacao robusta para provisionamento e observabilidade.
- Dependencia de estrategia de naming e governanca de lifecycle de banco.
