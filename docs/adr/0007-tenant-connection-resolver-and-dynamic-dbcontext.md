# ADR 0007: Resolução Dinâmica de Conexão com Cache Seguro e Fábrica de DbContext por Tenant

## Status

Aceito

## Contexto

A arquitetura do SaaS adota isolamento estrito de inquilinos com a estratégia **Database-per-Tenant**. Cada tenant contratante possui uma instância dedicada de banco SQL Server, cujas credenciais e connection strings são armazenadas criptografadas com AES-256 no banco central de catálogo (`MasterDb`).

Durante o ciclo de vida de uma requisição HTTP ou processamento assíncrono em background:
1. O tenant contextual é identificado no início da execução pelo pipeline HTTP (`ITenantContextAccessor`).
2. Operações de persistência precisam conectar-se diretamente ao banco do tenant correspondente.
3. Consultar o `MasterDb` e decifrar a connection string em cada operação ou injeção de dependência introduziria latência excessiva, sobrecarga desnecessária no banco de catálogo e custo de CPU na decriptografia contínua.
4. Além disso, a injeção síncrona do `DbContext` em pipelines de DI convencionais poderia incorrer no anti-padrão de *sync-over-async*, causando risco de thread pool starvation sob alta concorrência.

## Decisão

Adotamos a seguinte arquitetura de resolução e fábrica dinâmica:

1. **Contrato `ITenantConnectionResolver`:**
   - Define a interface centralizada no kernel compartilhado (`BuildingBlocks.Application.MultiTenancy`) para resolução assíncrona da connection string de inquilinos por identificador (`Guid`/`TenantId`), por subdomínio ou a partir do contexto contextual ativo (`ITenantContextAccessor`).
   - Retorna sempre o envelope padrão `Result<string>`.

2. **Implementação com Cache Seguro (`CachedTenantConnectionResolver`):**
   - Implementado em `Master.Infrastructure.Services`.
   - Utiliza `IMemoryCache` com política de expiração deslizante (30 minutos) e absoluta (4 horas).
   - Consulta o catálogo `MasterDb` via `ITenantRepository` apenas em caso de cache miss.
   - Decifra a connection string armazenada através do `IEncryptionService` (AES-256-CBC).
   - Armazena o valor decifrado em memória apenas para tenants ativos, indexando por ID e por subdomínio.
   - Fornece métodos de invalidação programática de cache (`InvalidateCache`).

3. **Fábrica Dinâmica Assíncrona (`ITenantDbContextFactory<TContext>`):**
   - Implementada em `BuildingBlocks.Infrastructure.Persistence`.
   - Resolve dinamicamente a connection string do tenant e instancia `TenantDbContext` configurado via `DbContextOptionsBuilder.UseSqlServer(connectionString)`.
   - Evita *sync-over-async* ao garantir que toda resolução de credenciais seja 100% assíncrona.

4. **Holder de Conexão de Escopo (`ITenantConnectionHolder`):**
   - Fornece armazenamento volátil por escopo (`Scoped`) para reaproveitamento imediato da connection string durante o mesmo ciclo de requisição.

## Consequências

### Positivas:
- **Performance e Escalabilidade:** Redução de até 99% nas consultas de catálogo para tenants ativos em tráfego recorrente.
- **Segurança Blindada:** Connection strings em repouso continuam integralmente cifradas com AES-256 no banco; descriptografia ocorre somente em memória e com expiração controlada.
- **Desacoplamento Modular:** O Kernel compartilhado depende apenas de abstrações; a persistência de catálogo permanece restrita ao módulo `Master`.
- **Isolamento de Dados Garantido:** Validado via testes de integração com Testcontainers SQL Server comprovando a total separação física entre bancos.

### Negativas / Mitigações:
- **Consistência de Cache na Troca de Credenciais:** Caso a connection string de um tenant seja rotacionada, deve-se invocar `InvalidateCache` para forçar recarga imediata.
- **Consumo de Memória:** O cache retém strings de conexão em memória; o volume é proporcional ao número de tenants ativos simultâneos no nó, o que é desprezível para a escala de memória da aplicação.
