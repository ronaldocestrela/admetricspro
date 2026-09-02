# ADR 0003: AES-256 for Tenant Connection String at Rest

## Status

Accepted

## Context

O MasterDb armazena metadados sensiveis de conectividade dos bancos de tenant. Esses dados precisam permanecer protegidos em repouso.

## Decision

Adotar criptografia simetrica AES-256-CBC com IV aleatorio por operacao para armazenar connection strings de tenant.

Regras:

- O payload cifrado e persistido em Base64.
- A chave criptografica e externa ao codigo fonte.
- Ambiente de teste usa chave dedicada de teste.

## Consequences

Positivas:

- Protecao de segredo em repouso no catalogo.
- Implementacao simples para operacoes de encrypt/decrypt no fluxo de provisionamento.

Negativas:

- Exige estrategia de rotacao de chave.
- Exige controle rigoroso de segredo em runtime.
