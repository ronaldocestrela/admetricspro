# ADR 0004: Padrão Result<T> e Erros Fortemente Tipados

## Status

Accepted

## Context

Em aplicações empresariais e arquiteturas limpas com Monólito Modular, o uso de exceções (`throw new Exception`) para sinalizar regras de negócio ou validações não atendidas acarreta diversos problemas:
- **Sobrecarga de Desempenho:** A criação de exceções e a captura de stack trace geram custo computacional significativo.
- **Opacidade nos Contratos:** Assinaturas de métodos que lançam exceções não comunicam de forma explícita aos consumidores quais falhas são esperadas.
- **Quebra do Fluxo de Controle:** O uso de blocos `try/catch` para controle de fluxo dispersa a lógica de tratamento e dificulta o mapeamento determinístico para respostas HTTP (`ProblemDetails` RFC 7807) ou tratamento na UI.

O documento `AGENTS.md` (item 3.1) estabelece como regra inegociável a proibição de exceções para regras de negócio e a obrigatoriedade do padrão `Result<T>`.

## Decision

Implementar as estruturas fundamentais `Result`, `Result<TValue>`, `Error` e o enum semântico `ErrorType` no kernel compartilhado (`BuildingBlocks.Domain.Primitives`):

1. **Estrutura `Result` & `Result<TValue>`:**
   - Encapsula `IsSuccess`, `IsFailure` e a instância associada de `Error`.
   - Garante a invariante estrita: tentar acessar a propriedade `Value` em um resultado que falhou lança `InvalidOperationException`.
   - Oferece fábricas semânticas (`Success`, `Failure`, `Create`) e operadores de conversão implícita de `TValue` e `Error` para `Result<TValue>`.
   - Fornece métodos funcionais de pattern-matching (`Match`) para projeções de retorno ou execuções direcionadas.

2. **Estrutura `Error` & `ErrorType`:**
   - Representa erros semânticos imutáveis através de um record contendo `Code`, `Description` e `Type` (`ErrorType`).
   - Categorias semânticas: `Failure` (500), `Validation` (400), `NotFound` (404), `Conflict` (409), `Unauthorized` (401), `Forbidden` (403).
   - Métodos de fábrica semânticos: `Error.Validation(...)`, `Error.NotFound(...)`, `Error.Conflict(...)`, `Error.Unauthorized(...)`, `Error.Forbidden(...)`.

3. **Invariantes do Construtor:**
   - Proíbe a instanciação de um resultado bem-sucedido com erro diferente de `Error.None`.
   - Proíbe a instanciação de um resultado com falha acompanhado de `Error.None`.

## Consequences

### Positivas:
- **Expressividade e Determinismo:** A assinatura de qualquer comando ou consulta explicita claramente que a operação pode falhar de forma previsível.
- **Eliminação de Exceções em Fluxo de Negócio:** Tratamento ágil e tipado em todas as camadas (Application, WebApi e Blazor).
- **Mapeamento HTTP Padronizado:** A presença de `ErrorType` viabiliza conversores genéricos para status HTTP semânticos (200, 400, 404, 409, etc.) e contratos OpenAPI transparentes.
- **100% Coberto por Testes Unitários:** Implementado sob TDD estrito com suíte dedicada em `tests/UnitTests/Backend/Primitives/`.

### Negativas:
- **Necessidade de Verificação Explícita:** Os consumidores são obrigados a inspecionar `result.IsSuccess` ou utilizar `Match` antes de ler `result.Value`.
- **Atenção em Tipos Nulos:** Conversões implícitas tratam valores nulos como falhas com `Error.NullValue`.
