# ADR 0019: Isolamento do Backoffice como Aplicação Dedicada e Autenticação com ASP.NET Core Identity no MasterDb

## Status
Aceito (Accepted) — 2026-09-04

## Contexto
O SaaS AdMetricsPro requer que todo o console administrativo (Backoffice) seja restrito exclusivamente a operadores autenticados e autorizados. As credenciais do operador inicial devem ser provisionadas via seed a partir do arquivo `.env` para facilitar o setup local e o deploy em contêineres Docker. Além disso, para evitar acoplamento de sessão, de layouts e de rotas entre os usuários finais dos tenants e a diretoria/suporte corporativo, fez-se necessária a separação física do Backoffice em relação ao portal do cliente (`WebApp`).

## Decisão
1. **Aplicação Dedicada (`src/Frontend/BackofficeApp`):**  
   Criou-se uma aplicação Blazor Server .NET 10 autônoma para o Backoffice, escutando em porta própria (HTTPS 7002 / HTTP 5002), com layout executivo dark mode e esquemas de cookie de sessão isolados (`AdMetricsPro_Backoffice_Session`).
2. **ASP.NET Core Identity no MasterDb:**  
   Adotou-se o pacote `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.11 no `Master.Infrastructure`. O `MasterDbContext` passou a herdar de `IdentityDbContext<MasterUser, MasterRole, Guid>`, garantindo que operadores globais fiquem segregados dos bancos individuais de dados dos tenants.
3. **Seed Idempotente via .env:**  
   O serviço `MasterIdentitySeeder` foi integrado ao ciclo de inicialização após as migrações EF Core, criando as roles (`SuperAdmin`, `SupportTechnician`) e o usuário Super Admin caso não existam, gerando hash de senha seguro via PBKDF2/Identity e registrando evento de auditoria imutável via `IMasterAuditService`.
4. **Proteção Completa de Telas:**  
   Todas as rotas do Backoffice foram envolvidas por `<AuthorizeRouteView>` com redirecionamento para a tela de login (`/login`), exibição de tela amigável de `/access-denied` e declaração de permissões `@attribute [Authorize(Roles = "SuperAdmin")]`.

## Consequências
### Positivas
- **Segurança de Alto Nível:** Proteção total contra acesso não autorizado a dados críticos de tenants, faturamento e disjuntores de API.
- **Desacoplamento Estrutural:** A aplicação do cliente (`WebApp`) não contém telas, layouts ou cookies de governança corporativa.
- **Configuração Simples em Ambientes Locais:** Seed automatizado que lê do `.env` sem necessidade de scripts manuais em SQL.
- **Conformidade com AGENTS.md:** 100% de cobertura de testes (TDD), Result<T>, XML docs e auditoria rastreável.

### Negativas / Mitigações
- Requer a inicialização de um processo executável adicional durante o desenvolvimento local (mitigado por perfis de execução documentados no README e launchSettings).
