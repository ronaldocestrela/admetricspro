# 0043 — Bounded context SocialPublishing e banco dedicado por tenant

## Status

Aceito em 2026-11-02.

## Contexto

A plataforma AdMetricsPro precisa de um módulo de **publicação orgânica** (Instagram, Facebook Pages, TikTok Content Posting) com calendário, aprovação white-label, IA editorial e handoff opcional para mídia paga. Esse fluxo não pode reutilizar o OAuth vault nem os `DbContext` operacionais de campanhas pagas (`Integrations`, `Analytics`), sob risco de acoplamento de escopos, vazamento de tokens e migrações cruzadas no mesmo schema.

O monólito modular já adota database-per-tenant (ADR 0002), resolução dinâmica de conexão (ADR 0007), mensageria in-memory MediatR (ADR 0009) e frontend estritamente via API (ADR 0020). O cofre de anúncios está definido no ADR 0026.

## Decisão

1. **Novo bounded context `SocialPublishing`** com quatro assemblies: `Domain`, `Contracts`, `Application` e `Infrastructure`, registrados na solution e compostos pela `WebApi` (Application + Infrastructure apenas). O `WebApp` Blazor **não** referencia `SocialPublishing.Infrastructure`.

2. **Limites de referência (guardrails automatizados):**
   - `Domain`: somente `BuildingBlocks.Domain`; sem EF Core, ASP.NET Core ou `*.Infrastructure`.
   - `Application`: `Domain`, `Contracts`, `BuildingBlocks.Application`; sem `Infrastructure`, `WebApi`, `Integrations.*`, `Analytics.*`, `Tenants.Infrastructure`.
   - `Infrastructure`: `Application`, `BuildingBlocks.Infrastructure`; sem `Integrations.Infrastructure` nem `Analytics.Infrastructure`.
   - `Contracts`: somente `BuildingBlocks.Domain` e `BuildingBlocks.Application` (comandos/notificações de integração futuros, sem EF).

3. **Persistência (Subfase 1.3):** dados do módulo no SQL Server do tenant, com `ITenantConnectionResolver` e `ITenantDbContextFactory<SocialPublishingDbContext>` implementado por `SocialPublishingDbContextFactory` (helper único `SocialPublishingDbContext.CreateOptions` com `MigrationsHistoryTable("__EFMigrationsHistory", "social")` e `HasDefaultSchema("social")`). Histórico de migrações exclusivo em `social.__EFMigrationsHistory`; migrações operacionais continuam em `dbo.__EFMigrationsHistory` do `TenantOperationalDbContext`. Proibido usar `TenantOperationalDbContext`, `ITenantDbContextAccessor` operacional ou LINQ direto em `OAuthTokenVault`, `Workspace` e `TenantBranding`. Proibido `Ignore` ou mapeamento cruzado de entidades operacionais no modelo EF do SocialPublishing.

   Tabelas iniciais no schema `social`: `OrganicConnections`, `OrganicPosts`, `OrganicMediaAssets`, `BrandVoiceProfiles`. Listas `MandatoryLinks` e `ForbiddenTerms` do Brand Voice persistem como JSON (`MandatoryLinksJson`, `ForbiddenTermsJson`); pesos de pilares como colunas owned. Tokens orgânicos permanecem somente como ciphertext nas colunas `EncryptedAccessToken` / `EncryptedRefreshToken`.

   **Subfase 2.4 (mídia orgânica):** `OrganicMediaAssets` passa a armazenar metadados de upload standalone (`WorkspaceId`, `Format`, `ObjectKey`, `MimeType`, `DurationMilliseconds`, `AspectLabel`) com `OrganicPostId` **anulável** (mídia registrada antes do post). O binário **nunca** é persistido em coluna SQL; fica na porta `IOrganicMediaStore` (disco local ou S3-compatível).

   Composição na **WebApi** (Master não referencia Social): decorator `SocialPublishingTenantProvisioningDecorator` sobre `ITenantProvisioningService` aplica `ISocialPublishingSchemaMigrator` após provisionamento operacional; `SocialPublishingTenantMigrationHostedService` itera tenants via `GetTenantsQuery` quando `DatabaseMigrations:ApplyMasterMigrationsOnStartup` está habilitado (exceto ambiente `Testing`).

4. **Cofre orgânico:** tokens de conexão orgânica cifrados com `IEncryptionService` (AES-256-CBC do kernel), envelope de propósito `organic:{platform}:{workspaceId}:{connectionId}` — separado do vault de mídia paga (ADR 0026).

5. **Integração entre módulos:** consultas de workspace, branding e catálogo permanecem nos módulos donos; a raiz de composição (`WebApi`, workers) despacha MediatR e, quando necessário, comandos tipados em `SocialPublishing.Contracts`. `Integrations.Application` e `Analytics.Application` poderão referenciar **apenas** `SocialPublishing.Contracts`, nunca Domain/Application/Infrastructure de Social.

6. **Composição:** `AddSocialPublishingModule(IServiceCollection, IConfiguration)` registra `AddMessaging` no assembly `SocialPublishing.Application`.

## Consequências

### Positivas

- Isolamento claro entre orgânico e mídia paga; escopos OAuth e modelos de dados não se misturam.
- Migrações do módulo não colidem com `__EFMigrationsHistory` operacional.
- Testes de conformidade (`AgentsArchitectureComplianceTests`, `XmlDocumentationComplianceTests`) impedem regressão de fronteiras.

### Negativas / mitigações

- Dois contextos EF no mesmo banco de tenant aumentam operações de migração e provisionamento.
  - *Mitigação:* aplicar `MigrateAsync` do contexto Social no pipeline de provisionamento e hosted service de startup (Subfase 1.3), alinhado ao ADR 0008.
- Handlers na WebApi precisam compor dados de vários módulos.
  - *Mitigação:* manter orquestração na raiz de composição; SocialPublishing expõe apenas seus casos de uso via MediatR.
