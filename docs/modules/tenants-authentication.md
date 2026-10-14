# Especificação de Módulo: Autenticação de Inquilinos (`TenantAuthService`)

Este documento descreve a arquitetura, segurança e fluxos do subsistema de autenticação de usuários e gestores de agências (tenants operacionais), integrados via **Monólito Modular** nos projetos `Tenants.Application` e `Tenants.Infrastructure`, em estrita conformidade com o [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

---

## 1. Visão Geral e Princípios Arquiteturais

A autenticação de inquilinos opera sob o modelo **Database-per-Tenant**, onde as credenciais transacionais dos operadores de agências residem no banco dedicado (`TenantDbContext`), enquanto a verificação de existência e adimplência do inquilino é realizada no catálogo central (`MasterDb`).

1. **Isolamento de Persistência (Database-per-Tenant):** O catálogo `MasterDb` armazena os metadados cadastrais, status e a string de conexão criptografada com AES-256 (`EncryptedConnectionString`). O banco operacional dedicado do inquilino (`TenantDbContext`) armazena os registros de `TenantUsers` e `TenantBranding`.
2. **Resolução de Identidade Híbrida:** A identificação do inquilino pode ocorrer via subdomínio na URL (`SubdomainTenantIdentificationStrategy`), header HTTP `X-Tenant-Id` (`HeaderTenantIdentificationStrategy`), ou parâmetro explícito no corpo da requisição (`TenantLoginApiRequest.Subdomain`).
3. **Segurança Criptográfica com Salt Dinâmico:** As senhas dos usuários de inquilino são validadas contra hashes gerados por `IPasswordHasher` (PBKDF2 com derivação HMAC-SHA256 e salting dinâmico de 128 bits).
4. **Pattern Result<T> Estrito:** Proibido o uso de exceptions para controle de fluxo de negócio. Todas as operações retornam envelopes `Result<T>`.
5. **Token JWT Assinado com Claims Ricas:** Emissão de tokens JWT com assinatura HMAC-SHA256 contendo `sub` (UserId), `email`, `name`, `role`, `tenant_id` e `tenant_subdomain`, sendo 100% interoperável com `JwtClaimTenantIdentificationStrategy`.

---

## 2. Papéis de Governança no Inquilino (`TenantRole`)

| Papel (Role) | Descrição | Escopo de Acesso |
| :--- | :--- | :--- |
| `Owner` | Proprietário / Sócio da Agência | Acesso irrestrito a todos os Workspaces, Squads, Faturamento, White-Label e Configurações da Agência. |
| `Admin` | Administrador da Agência | Gestão de colaboradores, Workspaces e conexões de APIs de anúncios. |
| `MediaBuyer` | Gestor de Tráfego / Analista | Operação de campanhas e visualização de métricas dos clientes atribuídos ao seu Squad. |
| `AccountManager` | Atendimento / CS | Acompanhamento de relatórios e atendimento de clientes dos Workspaces designados. |
| `Financial` | Financeiro da Agência | Gestão de orçamentos, conciliação e exportação de faturas. |
| `Guest` | Convidado / Observador | Visualização restrita em modo somente-leitura. |

---

## 3. Endpoints e Contratos de Comunicação

### 3.1 Autenticação de Usuário do Inquilino (`POST /api/v1/tenants/auth/login`)

#### Payload de Entrada (JSON)
```json
{
  "email": "carlos@vanguarda.com.br",
  "password": "SenhaForte123!#",
  "subdomain": "vanguarda"
}
```
*Nota: O campo `subdomain` é opcional se a requisição for enviada diretamente pelo subdomínio configurado (ex: `https://vanguarda.admetricspro.com.br`) ou com o cabeçalho `X-Tenant-Id: vanguarda`.*

#### Resposta de Sucesso (HTTP 200 - Envelope `Result<T>`)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "description": null,
    "type": 0
  },
  "value": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 28800,
    "userId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "email": "carlos@vanguarda.com.br",
    "fullName": "Carlos Gestor",
    "role": "Owner",
    "tenantId": "c5e8bd57-7492-4b54-8292-0ea97ac8c6a3",
    "subdomain": "vanguarda",
    "branding": {
      "agencyName": "Agência Vanguarda Digital",
      "primaryColor": "#1E40AF",
      "secondaryColor": "#F59E0B",
      "lightLogoUrl": "https://cdn.admetricspro.com.br/logos/vanguarda-light.png",
      "darkLogoUrl": null,
      "faviconUrl": "https://cdn.admetricspro.com.br/favicons/vanguarda.ico"
    }
  }
}
```

#### Respostas de Falha de Negócio (HTTP 401 / 422 - Envelope `Result<T>`)

##### Credenciais Inválidas (HTTP 401)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Auth.InvalidCredentials",
    "description": "E-mail ou senha incorretos.",
    "type": 2
  }
}
```

##### Usuário Desativado (HTTP 401)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Auth.AccountInactive",
    "description": "Esta conta de usuário está desativada.",
    "type": 2
  }
}
```

##### Inquilino Suspenso ou Cancelado (HTTP 422)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Tenant.Inactive",
    "description": "O acesso deste inquilino está Suspended.",
    "type": 1
  }
}
```

##### Inquilino Não Identificado (HTTP 422)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Tenant.IdentifierRequired",
    "description": "O inquilino deve ser identificado via subdomínio, cabeçalho ou parâmetro de requisição.",
    "type": 1
  }
}
```

### 3.2 Branding Público do Inquilino (`GET /api/v1/tenants/auth/branding`)

Utilizado pela tela de login (`TenantLoginPage.razor`) para pré-carregar as cores da agência e o nome antes do envio das credenciais.

#### Parâmetros de Consulta (Query String)
* `subdomain` (string, obrigatório): Subdomínio do inquilino a ser resolvido (ex: `vanguarda`).

#### Resposta de Sucesso (HTTP 200 - Envelope `Result<T>`)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "description": null,
    "type": 0
  },
  "value": {
    "tenantId": "c5e8bd57-7492-4b54-8292-0ea97ac8c6a3",
    "companyName": "Agência Vanguarda Digital",
    "subdomain": "vanguarda",
    "customDomain": null,
    "primaryColor": "#1E40AF",
    "secondaryColor": "#F59E0B",
    "logoUrl": "https://cdn.admetricspro.com.br/logos/vanguarda-light.png",
    "isActive": true
  }
}
```

---

## 4. Variáveis de Ambiente e Configuração

```env
# ------------------------------------------------------------------------------
# 6. SEGURANÇA & TOKENS JWT DO INQUILINO (TENANT AUTH)
# ------------------------------------------------------------------------------
TenantJwt__SecretKey="tenants_super_secret_jwt_key_minimum_32_characters_admetricspro_2026!"
TenantJwt__Issuer="AdMetricsPro.Tenants"
TenantJwt__Audience="AdMetricsPro.ClientApp"
TenantJwt__ExpirationMinutes=480
```

---

## 5. Casos de Borda e Políticas de Segurança

1. **Proteção Contra Enumeração de Usuários:** Respostas de e-mail inexistente e de senha incorreta retornam rigorosamente o mesmo código de erro (`Auth.InvalidCredentials` com HTTP 401), prevenindo enumeração maliciosa de e-mails de clientes.
2. **Inadimplência Financeira (Dunning Engine Integration):** Inquilinos suspensos pelo motor de dunning no `MasterDb` são imediatamente impedidos de autenticar (`Tenant.Inactive`).
3. **Resolução de Banco Dinâmica & Pool Seguro:** O `ITenantDbContextFactory<TenantDbContext>` abre a conexão sob demanda no banco SQL Server individual do tenant com base na connection string descriptografada em memória pelo `ITenantConnectionResolver` com cache LRU com expiração deslizante.


## Interface de login com a identidade da landing page

A rota `/login` usa fundo `#01030f`, superfícies escuras, tipografia Inter, ícones vetoriais e CTAs com gradiente azul/ciano. O CSS de `TenantLoginPage.razor.css` é isolado pelo compilador Razor, evitando que os estilos dos campos afetem o onboarding ou o dashboard.

O cabeçalho fixo de 76px apresenta a mesma marca e o asset `images/landing/brand-mark.svg` da LP. Logo e nome formam um link para `/`. O botão “Crie seu Cockpit” no cabeçalho e no rodapé do formulário leva a `/onboarding`, sem promessa de tempo de implantação. O botão de autenticação exibe “Entrar no Cockpit”, sem seta.

No formulário padrão, logo e nome aparecem juntos, com a identidade institucional. Quando há branding de tenant, o nome, logo e variáveis de tema carregados pelo cliente HTTP continuam sendo utilizados no card. O botão de visibilidade preserva a senha e informa “Mostrar senha” ou “Ocultar senha”, com `aria-controls` e `aria-pressed`.

A mudança é de apresentação. Os contratos HTTP, validações, persistência da sessão, resolução de subdomínio e redirecionamento após autenticação seguem o fluxo existente. Não houve novo endpoint, acesso direto a banco ou alteração de regras de autenticação. As transições respeitam movimento reduzido e a composição adapta o cabeçalho, card e sufixo do subdomínio para telas pequenas.

Os testes de marca, navegação e visibilidade foram executados com falha antes da alteração. `TenantLoginPageTests` também mantém a cobertura de branding, erros de validação, falha de autenticação e login bem-sucedido.

Validação da atualização visual: 283 testes de frontend aprovados. A conferência no Chrome validou desktop de 1440px e larguras de 1024px, 390px e 320px, sem sobreposição dos controles do cabeçalho ou rolagem horizontal. Foram verificados navegação à LP e ao cadastro, alternância da visibilidade da senha e erro local de campos obrigatórios, sem autenticar uma conta real.
