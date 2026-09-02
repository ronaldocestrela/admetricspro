# ADR 0005: Abstrações Base de Domínio (DDD) e Contratos de Persistência

## Status

Accepted

## Context

Para sustentar a arquitetura de **Monólito Modular** com isolamento estrito de módulos e multitenancy por banco de dados, o sistema necessita de uma fundação sólida de Domain-Driven Design (DDD) no Kernel Compartilhado (`BuildingBlocks`).

Sem tipos base padronizados para entidades, agregados e objetos de valor:
- Cada módulo poderia adotar convenções discrepantes para identificação de entidades e igualdade estrutural.
- A emissão e despacho de eventos de domínio ficaria acoplada a mecanismos de persistência ou bibliotecas de terceiros diretamente no coração do domínio.
- Os repositórios poderiam vazar detalhes de implementação do ORM (EF Core) para a camada de aplicação ou domínio.

As diretrizes do `AGENTS.md` (itens 1, 3.3 e 3.4) determinam:
1. Agregados devem ter controle transacional explícito.
2. Contratos de repositório devem ser genéricos e tipados por agregado.
3. Operações de escrita devem ser consolidadas através do `IUnitOfWork.CommitAsync(CancellationToken)`.
4. Suporte obrigatório a `CancellationToken` em todas as operações assíncronas.

## Decision

Implementar as abstrações de domínio em `BuildingBlocks.Domain.Abstractions` e os contratos de persistência em `BuildingBlocks.Application.Persistence`:

1. **`Entity<TId>`:**
   - Classe base com suporte a identificadores fortemente tipados (`where TId : notnull`).
   - Implementa `IEquatable<Entity<TId>>` e sobrecarrega `Equals`, `GetHashCode`, operadores `==` e `!=`.
   - A igualdade entre entidades é estritamente baseada no tipo em runtime e no `Id`.

2. **`AggregateRoot<TId>` & `IDomainEvent`:**
   - Raiz de agregado que herda de `Entity<TId>`.
   - Encapsula a coleção interna de eventos de domínio, expondo-a de forma somente leitura (`IReadOnlyCollection<IDomainEvent> DomainEvents`).
   - Fornece métodos `RaiseDomainEvent` (protegido) e `ClearDomainEvents` (público para o interceptor de persistência).
   - `IDomainEvent` desacoplado do MediatR no nível de domínio, expondo metadados fundamentais (`EventId` e `OccurredOnUtc`).

3. **`ValueObject`:**
   - Classe base abstrata para objetos de valor com igualdade estrutural baseada em `GetEqualityComponents()`.
   - Implementa `IEquatable<ValueObject>` com suporte seguro a propriedades nulas, tipos primitivos e sequências.

4. **`IRepository<TEntity, TId>` & `IUnitOfWork`:**
   - `IRepository` restrito a agregados (`where TEntity : AggregateRoot<TId> where TId : notnull`).
   - Métodos: `AddAsync`, `GetByIdAsync`, `Update` e `Remove`, com `CancellationToken = default`.
   - `IUnitOfWork` expondo `CommitAsync(CancellationToken cancellationToken = default)` para unificar a fronteira transacional de escrita.

## Consequences

### Positivas:
- **Consistência Semântica:** Todos os módulos de negócio herdam comportamentos uniformes de ciclo de vida e igualdade.
- **Desacoplamento de Infraestrutura:** O domínio não depende de EF Core ou de pacotes externos para modelar agregados ou eventos.
- **Transações Determinísticas:** Alterações em múltiplos agregados respeitam a unidade de trabalho via `IUnitOfWork.CommitAsync()`.
- **100% Testado com TDD:** Cobertura abrangente em `tests/UnitTests/Backend/Abstractions/` e `tests/UnitTests/Backend/Persistence/`.
- **Zero Alertas de Compilação:** Compilação em conformidade estrita com `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

### Negativas:
- **Sobrecarga de Implementação em ValueObjects:** Classes derivadas de `ValueObject` precisam implementar o método gerador `GetEqualityComponents()`.
