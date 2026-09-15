# ADR 0025: Estratégia de Resolução Dinâmica de CNAME e Customização White-Label

## Status
Aceito

## Data
2026-09-15

## Contexto
O modelo de negócio do AdMetricsPro atende agências de performance, gestores de tráfego e anunciantes corporativos que exigem:
1. **Identidade Visual Customizada (White-Label):** Capacidade de personalizar paleta de cores institucional, logotipos específicos para temas claro e escuro, além de favicon do navegador nos portais e dashboards compartilhados.
2. **Domínio Próprio (CNAME Dinâmico):** Possibilidade de acessar a plataforma através de um FQDN próprio da agência (ex: `relatorios.agenciaalfa.com.br`) apontando para a infraestrutura do SaaS via registro CNAME (`cname.admetricspro.com`).
3. **Isolamento de Responsabilidades e Multitenancy Estrito:**
   - As customizações visuais operacionais devem pertencer exclusivamente ao banco de dados dedicado do inquilino (`TenantDbContext`).
   - A governança de domínios, roteamento e resolução dinâmica via HTTP deve consultar o catálogo global (`MasterDbContext`), com garantia de unicidade de domínio e verificação de privilégios de plano (`PlanFeatures.HasCustomCname`).
4. **Performance e Resiliência:** A resolução do inquilino a partir do cabeçalho `Host` ocorre em cada requisição HTTP de entrada, tornando inaceitável consultas frequentes e diretas ao banco de dados mestre sem camada de cache.

## Decisão

### 1. Separação Híbrida de Persistência
- **`TenantDbContext` (Banco Dedicado):** Armazena a entidade `TenantBranding` com cores hexadecimais validadas, URLs de logotipo claro/escuro e favicon.
- **`MasterDbContext` (Banco Catálogo):** Armazena a coluna `CustomDomain` na tabela `Tenants`, com índice único para prevenção de colisões entre inquilinos.

### 2. Estratégia de Identificação Dinâmica (`CustomDomainTenantIdentificationStrategy`)
- Implementa `ITenantIdentificationStrategy` com prioridade após headers explícitos e tokens JWT.
- Inspeciona o cabeçalho `Host` da requisição HTTP (removendo números de porta).
- Ignora domínios institucionais padrão (`admetricspro.com`, `app.admetricspro.com`, `localhost`).
- Utiliza cache in-memory (`IMemoryCache`) com TTL de 5 minutos e expiração deslizante de 1 minuto para armazenar o mapeamento `TenantCustomDomainMapping(TenantId, Subdomain, CustomDomain)`.
- Adiciona o enum `TenantResolutionSource.CustomDomain = 4` para auditoria do método de resolução.

### 3. Validação de Regras de Negócio e Governança de Planos
- O comando `ConfigureTenantCustomDomainCommand` valida:
  - Formato estrito de FQDN via Regex (rejeita protocolos, barras, portas e termos curtos).
  - Capacidade do plano contratado (`plan.Features.HasCustomCname`), restringindo domínios personalizados a planos compatíveis (Enterprise).
  - Unicidade global através de consulta prévia no catálogo `MasterDb`.

### 4. Consumo no Frontend Blazor Server (Zero Acesso Direto a Banco)
- O frontend consome exclusivamente os endpoints da Web API:
  - `/api/v1/tenants/branding` (`GET`, `PUT`)
  - `/api/v1/tenants/cname` (`GET`, `PUT`, `DELETE`)
- Utiliza os clientes tipados `ITenantBrandingClientService` e `ITenantCnameClientService`.
- A página `/settings/white-label` (`WhiteLabelSettingsPage.razor`) disponibiliza *live preview* em tempo real com simulador visual e instruções de apontamento de DNS (Tipo: `CNAME`, Alvo: `cname.admetricspro.com`).

## Consequências

### Positivas
- Total flexibilidade para agências utilizarem sua própria marca sem fricção.
- Resolução de tenant de altíssima performance graças ao cache de domínios em memória.
- Respeito integral aos limites arquiteturais (Regras 1, 3, 5, 8 e 9 do `AGENTS.md`).
- Cobertura total de testes unitários e de componentes com bUnit.

### Negativas / Mitigações
- Necessidade de configuração e propagação prévia de DNS pelo cliente da agência: mitigado por instruções visuais claras e detalhadas na interface administrativa.
- Em ambientes distribuídos de produção com múltiplas instâncias da API Web, a invalidação do cache in-memory pode levar até 5 minutos para refletir: aceitável para operações esporádicas de alteração de domínio.
