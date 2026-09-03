# ADR 0006: Pipeline de Resolução Dinâmica de Tenant & Contexto Multitenant

## Status

Accepted

## Context

Para viabilizar a arquitetura **Database-per-Tenant** descrita no `AGENTS.md` (item 3.2), o sistema necessita determinar dinamicamente qual inquilino está operando em cada requisição HTTP recebida pela WebApi e interfaces Blazor Server.

Sem um mecanismo unificado e resiliente de resolução de inquilino:
- Cada módulo ou endpoint teria que implementar lógicas próprias de parsing de headers, tokens JWT ou subdomínios, gerando duplicação e potenciais brechas de segurança.
- Não haveria uma fronteira clara e isolada para requisições concorrentes, arriscando vazamento de contexto entre threads ou requisições.
- Requisições anônimas ou voltadas ao catálogo mestre (`MasterDb`), como `/health` ou provisionamento inicial, poderiam falhar indevidamente caso o middleware tratasse a ausência de inquilino como exceção fatal.

As diretrizes do `AGENTS.md` exigem:
1. **Identificação Multi-Canal:** Suporte simultâneo a Header HTTP (`X-Tenant-Id`), Claim em Token JWT e Subdomínio/CNAME.
2. **Isolamento de Escopo:** Injeção do `ITenantContext` no escopo da requisição (`Scoped`).
3. **Ausência de Exceções de Fluxo:** Tratamento graceful de rotas públicas ou não resolvidas sem lançamento de exceções.
4. **TDD Estrito:** Desenvolvimento orientado a testes com 100% de cobertura nos canais de extração e no middleware.

## Decision

Implementar a infraestrutura de resolução dinâmica no Kernel Compartilhado (`BuildingBlocks`):

1. **Abstrações em `BuildingBlocks.Application.MultiTenancy`:**
   - `TenantResolutionSource` (Enum): `None`, `Header`, `JwtClaim`, `Subdomain`.
   - `ITenantContext` (Interface): Exposição somente leitura dos identificadores do inquilino (`TenantId`, `Subdomain`, `RawIdentifier`, `Source`, `IsResolved`).
   - `ITenantContextAccessor` (Interface): Contrato para leitura e atribuição do contexto no escopo.

2. **Infraestrutura e Pipeline em `BuildingBlocks.Infrastructure.MultiTenancy`:**
   - `TenantContext`: Implementação imutável com padrão factory (`TenantContext.Create` e `TenantContext.Empty`).
   - `TenantContextAccessor`: Implementação híbrida combinando armazenamento por escopo com `AsyncLocal` e *ContextHolder Pattern*, permitindo propagação determinística em chamadas assíncronas sem vazamento de contexto entre fluxos concorrentes.
   - `ITenantIdentificationStrategy`: Contrato de estratégia assíncrono para canais de extração (`HeaderTenantIdentificationStrategy`, `JwtClaimTenantIdentificationStrategy`, `SubdomainTenantIdentificationStrategy`).
   - `TenantIdentificationMiddleware`: Middleware ASP.NET Core que executa a cadeia de estratégias respeitando a ordem de precedência configurável (`Header` > `JwtClaim` > `Subdomain`) e popula o `TenantContext`.
   - `MultiTenancyServiceExtensions`: Métodos de extensão fluentes `AddMultiTenancy()` e `UseTenantResolution()` para integração transparente na inicialização da aplicação.

## Consequences

### Positivas:
- **Resolução Unificada e Transparente:** Qualquer serviço, repositório ou DbContext contextual tem acesso imediato ao `ITenantContext` apenas injetando a interface por escopo.
- **Suporte Multi-Canal Completo:** Suporta chamadas de API com headers, autenticação JWT com claims padronizadas e acesso via subdomínio/CNAME direto na URL.
- **Segurança e Isolamento Total:** Contextos são isolados por requisição e fluxo assíncrono, prevenindo contaminação cruzada de dados entre clientes.
- **Resiliência e Ausência de Exceções:** Requisições sem inquilino permanecem com `IsResolved == false`, permitindo que rotas centrais do `MasterDb` operem livremente.
- **100% Testado via TDD:** Suíte abrangente em `tests/UnitTests/Backend/MultiTenancy/` cobrindo todas as estratégias, precedências e cenários de borda.
- **Zero Warnings de Compilação:** Plena aderência a `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` e documentação XML integral.

### Negativas:
- **Resolução de Subdomínio Local:** Ambientes locais com portas customizadas e hosts locais exigem configuração explícita de `BaseDomains` (como `localhost`).
