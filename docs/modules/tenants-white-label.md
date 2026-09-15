# Módulo Tenants & Master — Personalização White-Label e CNAME Dinâmico

Este documento descreve a arquitetura, regras de negócio, fluxos de persistência híbrida (`TenantDbContext` e `MasterDbContext`), especificações de endpoints e diretrizes de frontend para a funcionalidade de **White-Label e Apontamento de CNAME Dinâmico** no SaaS **AdMetricsPro**.

---

## 1. Visão Geral e Arquitetura

O módulo White-Label permite que agências de mídia e grandes anunciantes customizem integralmente a experiência visual da plataforma e utilizem seus próprios domínios personalizados (FQDNs), proporcionando uma experiência imersiva e descaracterizada da marca AdMetricsPro.

### 1.1 Persistência Híbrida e Bounded Contexts
1. **Identidade Visual no Banco Dedicado (`TenantDbContext`):**
   - Cores institucionais (primária e secundária).
   - Logomarcas dedicadas para os modos claro (*light*) e escuro (*dark*).
   - Favicon do navegador.
   - Tabela `TenantBranding` no schema operacional do inquilino.
2. **Roteamento e Catálogo no Banco Central (`MasterDbContext`):**
   - O campo `CustomDomain` na entidade `Tenant` do banco `MasterDb`.
   - Garantia de unicidade global de domínios customizados entre todos os inquilinos.
   - Validação de autorização baseada nas funcionalidades do plano (`PlanFeatures.HasCustomCname`).

---

## 2. Resolução de Tenant Dinâmica por CNAME

O pipeline de identificação multitenant foi estendido com a estratégia `CustomDomainTenantIdentificationStrategy` implementando `ITenantIdentificationStrategy`:

### Fluxo de Identificação no Middleware HTTP:
1. O middleware inspeciona o cabeçalho `Host` da requisição HTTP (ex: `relatorios.agenciaalfa.com.br`).
2. Desconsidera portas e remove sufixos inválidos.
3. Se o host for o domínio raiz institucional do SaaS (`admetricspro.com` ou `localhost`), a estratégia ignora a resolução.
4. Consulta em cache in-memory (`IMemoryCache` com TTL de 5 minutos e expiração deslizante de 1 minuto) para evitar overhead no banco `MasterDb`.
5. Em caso de *cache miss*, o serviço `ITenantCustomDomainResolver` consulta o catálogo MasterDb (`GetByCustomDomainAsync`) e armazena o mapeamento `TenantCustomDomainMapping(TenantId, Subdomain, CustomDomain)`.
6. O `TenantContext` é populado com `Source = TenantResolutionSource.CustomDomain (4)`.

---

## 3. Endpoints da Web API

### 3.1 Obter Branding Ativo
- **Rota:** `GET /api/v1/tenants/branding`
- **Sumário OpenAPI:** `Obtém parâmetros completos de White-Label do inquilino autenticado`
- **Códigos HTTP:** `200 OK`, `401 Unauthorized`

#### Retorno de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "primaryColor": "#2563EB",
    "secondaryColor": "#0F172A",
    "lightLogoUrl": "https://cdn.agenciaalfa.com.br/logo-light.svg",
    "darkLogoUrl": "https://cdn.agenciaalfa.com.br/logo-dark.svg",
    "faviconUrl": "https://cdn.agenciaalfa.com.br/favicon.ico",
    "updatedAtUtc": "2026-09-15T19:30:00Z"
  }
}
```

---

### 3.2 Atualizar Identidade Visual White-Label
- **Rota:** `PUT /api/v1/tenants/branding`
- **Sumário OpenAPI:** `Atualiza a identidade visual White-Label do inquilino autenticado`
- **Códigos HTTP:** `200 OK`, `400 BadRequest`, `401 Unauthorized`, `422 UnprocessableEntity`

#### Payload de Entrada (`UpdateTenantBrandingApiRequest`):
```json
{
  "primaryColor": "#10B981",
  "secondaryColor": "#1E293B",
  "lightLogoUrl": "https://cdn.agenciaalfa.com.br/logo-light.svg",
  "darkLogoUrl": "https://cdn.agenciaalfa.com.br/logo-dark.svg",
  "faviconUrl": "https://cdn.agenciaalfa.com.br/favicon.ico"
}
```

#### Regras de Validação:
- Cores devem estar no formato hexadecimal `#RRGGBB` ou `#RGB`.
- URLs de logomarcas devem ter esquema `http` ou `https` e extensões permitidas: `.png`, `.svg`, `.jpg`, `.jpeg`, `.webp`.
- Favicon deve possuir extensões: `.ico`, `.png`, `.svg`.

---

### 3.3 Obter Configurações e Instruções DNS de CNAME
- **Rota:** `GET /api/v1/tenants/cname`
- **Sumário OpenAPI:** `Obtém o status do domínio CNAME e instruções DNS do inquilino ativo`
- **Códigos HTTP:** `200 OK`, `401 Unauthorized`, `404 NotFound`

#### Retorno de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "tenantId": "c3b9b46e-1d54-4740-9a25-c64988e4a9e5",
    "customDomain": "relatorios.agenciaalfa.com.br",
    "expectedCnameTarget": "cname.admetricspro.com",
    "isConfigured": true,
    "hasPlanSupport": true
  }
}
```

---

### 3.4 Configurar Domínio CNAME
- **Rota:** `PUT /api/v1/tenants/cname`
- **Sumário OpenAPI:** `Configura ou altera o domínio personalizado CNAME do inquilino`
- **Códigos HTTP:** `200 OK`, `400 BadRequest`, `401 Unauthorized`, `403 Forbidden`, `409 Conflict`, `422 UnprocessableEntity`

#### Payload de Entrada (`ConfigureTenantCustomDomainApiRequest`):
```json
{
  "customDomain": "relatorios.agenciaalfa.com.br"
}
```

#### Erros de Negócio Mapeados:
| Código de Erro | Status HTTP | Descrição |
| :--- | :--- | :--- |
| `Tenant.PlanLacksCustomCname` | `403 Forbidden` | O plano contratado não contempla domínios customizados (exclusivo Enterprise). |
| `Tenant.InvalidCustomDomain` | `422 Unprocessable` | Formato de FQDN inválido (contém protocolo, portas ou caracteres ilegais). |
| `Tenant.CustomDomainConflict` | `409 Conflict` | O domínio informado já está registrado por outro inquilino no catálogo. |
| `Tenant.NotFound` | `404 NotFound` | Inquilino não localizado no catálogo MasterDb. |

---

### 3.5 Remover Apontamento de CNAME
- **Rota:** `DELETE /api/v1/tenants/cname`
- **Sumário OpenAPI:** `Remove a vinculação do domínio CNAME personalizado do inquilino`
- **Códigos HTTP:** `200 OK`, `401 Unauthorized`, `404 NotFound`

---

## 4. Frontend Blazor Server (.NET 10)

### 4.1 Cabeçalho e Título Dinâmicos (`TenantMainLayout.razor`)
- Utiliza `<HeadContent>` nativo do Blazor para atualizar dinamicamente a tag `<title>` com o nome fantasia da agência e o `<link rel="icon">` com a URL do Favicon customizado.

### 4.2 Componente Atômico de Logomarca (`BrandLogo.razor`)
- Suporta alternância automática baseada no tema do usuário (Claro/Escuro).
- Possui fallback institucional com renderização em SVG e monograma em gradiente caso o cliente não tenha configurado imagem externa.

### 4.3 Tela de Configurações (`WhiteLabelSettingsPage.razor`)
- Localizada na rota `/settings/white-label`.
- Formulário com *color pickers* interativos, campos de URL com validação de extensão e *Live Preview* em tempo real.
- Card dedicado com instruções de DNS:
  - **Tipo:** `CNAME`
  - **Host:** `relatorios` (ou subdomínio desejado)
  - **Valor/Destino:** `cname.admetricspro.com`
- Trava visual e alerta amigável de upgrade caso o plano atual não suporte domínios customizados.
