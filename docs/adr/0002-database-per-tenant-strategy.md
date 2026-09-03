# ADR 0002: Estratégia de Isolamento Multitenant via Database-per-Tenant com Catálogo MasterDb

## Status

Accepted

## Contexto

A plataforma **AdMetricsPro** é um SaaS corporativo de gestão unificada de tráfego pago (Meta Ads, Google Ads, Bing Ads e TikTok Ads), que gerencia dados financeiros críticos, volumes massivos de métricas de anúncios, regras de automação em tempo real e tokens de acesso a APIs de terceiros.

Em ambientes multitenant, existem três abordagens clássicas de particionamento de dados:
1. **Banco de Dados Compartilhado com Coluna Discriminadora (`TenantId`):** Menor custo de infraestrutura, porém com alto risco de vazamento acidental de dados (cross-tenant data leak), complexidade em consultas analíticas pesadas e impossibilidade de restauração/backup pontual por cliente.
2. **Schema-per-Tenant (Schemas separados no mesmo banco SQL Server):** Isolamento lógico moderado, mas mantém contenção de recursos de I/O, concorrência no nível do motor e limites práticos de governança de schemas.
3. **Database-per-Tenant (Banco de dados dedicado por tenant) com Catálogo Central (`MasterDb`):** Isolamento físico estrito de dados, backups e restore independentes, conformidade regulatória simplificada (LGPD/GDPR com descarte irrecuperável por tenant), mitigação de efeito de "vizinho barulhento" (noisy neighbor) no nível do storage e possibilidade de alocar instâncias dedicadas para clientes corporativos de alto volume.

Adicionalmente, para viabilizar o roteamento transparente de conexões e a governança global, é imprescindível um banco de catálogo central (`MasterDb`) para registrar os metadados dos tenants, o status das assinaturas e suas respectivas credenciais seguras de acesso ao banco dedicado.

## Decisão

Adota-se a estratégia arquitetural **Database-per-Tenant** ancorada em um catálogo central (`MasterDb`), regida pelas seguintes definições técnicas:

1. **Catálogo Master (`MasterDbContext`):**
   - Um banco central dedicado (`MasterDb`) armazena a tabela `Tenants`, contendo identificadores únicos (`TenantId`), dados cadastrais (Razão Social, CNPJ), subdomínio exclusivo, status de ciclo de vida (`Active`, `Trial`, `Suspended`, `Cancelled`), tier de plano contratado e a Connection String do banco operacional do tenant criptografada com algoritmo simétrico AES-256 (`EncryptedConnectionString`).
   - O `MasterDbContext` aplica migrações automáticas de seu próprio schema no startup do host WebApi através do runner idempotente `IMasterDatabaseMigrationRunner`.

2. **Bancos Operacionais Dedicados dos Tenants (`TenantOperationalDbContext`):**
   - Cada tenant provisionado possui seu próprio banco de dados no SQL Server com nomenclatura padronizada e sanitizada (ex.: `AdMetrics_Tenant_{GUID}`).
   - Ao provisionar um novo tenant, o serviço `ITenantProvisioningService` cria fisicamente o banco no SQL Server e executa as migrações operacionais do `TenantOperationalDbContext` (registrando a tabela `__EFMigrationsHistory` no banco individual) antes de marcar o tenant como pronto para uso.

3. **Resolução Dinâmica de Conexão em Runtime:**
   - Durante as requisições HTTP e sessões SignalR do Blazor Server, o identificador do tenant é extraído pelo middleware de multitenancy (via header `X-Tenant-Id`, subdomínio CNAME ou claim de token JWT).
   - O serviço `ITenantConnectionResolver` busca a Connection String criptografada no `MasterDb`, descriptografa-a com a chave mestra de ambiente, aplica cache em memória de alta performance com invalidação por evento e fornece a string de conexão para a instância com escopo (Scoped) do `TenantOperationalDbContext`.

4. **Nenhum Compartilhamento Transacional:**
   - Módulos de domínio operacional e regras de automação nunca executam queries diretas contra o `MasterDb`. Apenas serviços autorizados de governança e backoffice administrativo interagem com o `MasterDbContext`.

## Consequências

### Positivas
- **Isolamento de Segurança Máximo:** Impossibilidade de vazamento de consultas entre tenants no nível de persistência relacional; um tenant não compartilha tabelas operacionais com outro.
- **Conformidade Legal Estrita (LGPD/GDPR):** O direito ao esquecimento e descarte de dados pode ser atendido através do descarte (`DROP DATABASE`) ou desanexação segura do banco do tenant, sem necessidade de rotinas lentas de `DELETE CASCADE` em tabelas com milhões de registros.
- **Manutenção e Backup Granulares:** Planos de backup e restauração (`Point-in-Time Recovery`) customizáveis por cliente, permitindo restaurar o ambiente de um único cliente sem afetar os demais.
- **Escalabilidade Diferenciada:** Clientes enterprise podem ter seus bancos alocados em nós ou discos SQL Server dedicados com maior provisionamento de IOPS e throughput.

### Negativas / Mitigações
- **Custo Operacional de Migrações em Massa:** A propagação de atualizações de schema operacional para dezenas ou centenas de bancos requer runners automatizados com observabilidade e tratamento de falhas individuais.
  - *Mitigação:* Implementação de pipeline de provisionamento e runners de migração com isolamento transacional, logs estruturados e envelope `Result<T>` para evitar exceções não capturadas.
- **Gestão de Conexões e Connection Pooling:** O pool de conexões do ADO.NET / SQL Server é segmentado por connection string, o que pode aumentar a contagem de pools abertos.
  - *Mitigação:* Monitoramento de pools de conexão, parametrização adequada de `Max Pool Size` e reuso eficiente de conexões com `IUnitOfWork`.
