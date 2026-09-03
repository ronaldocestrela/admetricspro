# AdMetricsPro 🚀

> **SaaS de Gestão Unificada de Tráfego Pago** (Meta Ads, Google Ads, Bing Ads e TikTok Ads), multitenant com isolamento por banco de dados (*Database-per-Tenant*), desenvolvido sob a arquitetura de **Monólito Modular** em **.NET 10**.

---

## 📌 Visão Geral da Solução

O **AdMetricsPro** é uma plataforma corporativa para consolidação, monitoramento, governança e automação de anúncios em múltiplas redes. O sistema é composto por:

- **Backend (Web API):** Construído em ASP.NET Core (.NET 10), expondo endpoints RESTful semânticos, governança do catálogo central (`MasterDb`), provisionamento de tenants, régua de dunning, monitoramento de saúde/rate limits de APIs de mídia e documentação OpenAPI com interface viva **Scalar UI**.
- **Frontend (Blazor Server):** Aplicação interativa em Blazor Server (.NET 10) com renderização reativa em tempo real (SignalR), suporte nativo a personalização **White-Label** (cores primárias, secundárias, logos, favicons e remoção de marca) e console administrativo completo (Backoffice 360°).
- **Isolamento Multitenant Estrito:** Modelo *Database-per-Tenant* em SQL Server, onde cada inquilino possui banco isolado e criptografado para dados transacionais, administrado a partir do catálogo central `MasterDb`.

---

## 🛠️ Tecnologias & Arquitetura

- **Linguagem & Runtime:** C# 14 / [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Host Web API:** ASP.NET Core 10 Web API
- **Frontend SPA:** Blazor Web App (Interactive Server Mode)
- **Persistência:** Entity Framework Core 10 com SQL Server
- **Documentação de API:** OpenAPI nativo do .NET 10 + [Scalar UI](https://scalar.com) em `/scalar/v1`
- **Padrão de Resposta:** `Result<T>` e erros tipados (sem lançamento de exceções de negócio)
- **Comunicação Inter-Módulos:** Mensageria in-memory com mediador desacoplado (`MediatR`)
- **Testes Automatizados:** xUnit, FluentAssertions, bUnit (componentes Blazor) e [Testcontainers](https://testcontainers.com) (SQL Server para testes de integração)

---

## 📋 Pré-requisitos

Para rodar o projeto localmente, certifique-se de ter instalado em sua máquina:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (versão `10.0.100` ou superior)
   ```bash
   dotnet --version
   ```
2. **[Docker Desktop](https://www.docker.com/)** ou Docker Engine com Docker Compose (para subir a instância local do SQL Server e rodar testes com Testcontainers)
3. **Git** para controle de versão

---

## 🚀 Como Rodar Localmente (Passo a Passo)

### 1. Clonar o Repositório

```bash
git clone https://github.com/ronaldocestrela/admetricspro.git
cd admetricspro
```

### 2. Configurar o Arquivo `.env` (Segredos e Variáveis Locais)

O projeto utiliza um arquivo `.env` para centralizar credenciais sensíveis (senhas do SQL Server, connection strings, chaves simétricas JWT de impersonação e parâmetros operacionais), garantindo que nenhum segredo seja comitado no controle de versão.

Crie seu arquivo `.env` local a partir do modelo pré-configurado:

```bash
cp .env.example .env
```

> 🔒 **Segurança Garantida:** O arquivo `.env` está explicitamente ignorado no [.gitignore](file:///home/rony/LPR/AdMetricsPro/.gitignore). O arquivo de exemplo [.env.example](file:///home/rony/LPR/AdMetricsPro/.env.example) contém descrições detalhadas de cada variável.

### 3. Subir o Banco de Dados SQL Server via Docker

Com o `.env` criado, o `docker-compose.yml` lerá automaticamente as variáveis de ambiente parametrizadas (`MSSQL_SA_PASSWORD` e `MSSQL_PORT`):

```bash
docker compose up -d
```

> **Credenciais padrão do container (parametrizadas no `.env`):**
> - **Host:** `localhost,1433`
> - **Usuário:** `sa`
> - **Senha:** `YourStrong@Passw0rd!`
> - **Volume persistente:** `sqlserver-data`

---

### 4. Resolução Automática de Configurações via `.env`

Tanto o **Backend (WebApi)** quanto o **Frontend (WebApp)** e os testes leem o arquivo `.env` automaticamente no startup, resolvendo:
- `ConnectionStrings__MasterDb`: Cadeia de conexão do banco de dados `MasterCatalog`.
- `DatabaseMigrations__ApplyMasterMigrationsOnStartup`: Quando `true`, executa migrações no startup da WebApi.
- `ImpersonationJwt__SecretKey`: Chave criptográfica simétrica HMAC-SHA256 para tokens de Shadow Mode.
- `Api__BaseUrl`: URL da API consumida pelo Blazor Server (`https://localhost:7001` ou `http://localhost:5000`).

Os arquivos de configuração padrão já estão preparados para o ambiente de desenvolvimento local:

#### Backend (`src/Backend/WebApi/appsettings.Development.json` ou `appsettings.json`)
```json
{
  "ConnectionStrings": {
    "MasterDb": "Server=localhost,1433;Database=MasterCatalog;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;"
  },
  "DatabaseMigrations": {
    "ApplyMasterMigrationsOnStartup": true
  }
}
```

> 💡 **Dica:** Ativar `"ApplyMasterMigrationsOnStartup": true` faz com que o WebApi aplique automaticamente todas as migrações do catálogo `MasterCatalog` na inicialização!

#### Frontend (`src/Frontend/WebApp/appsettings.Development.json` ou `appsettings.json`)
```json
{
  "ConnectionStrings": {
    "MasterDb": "Server=localhost,1433;Database=MasterCatalog;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;"
  },
  "Api": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

---

### 4. Executar o Backend (WebApi)

Abra um terminal e execute o projeto da API:

```bash
dotnet run --project src/Backend/WebApi/WebApi.csproj
```

Por padrão, a API será iniciada e estará acessível em:
- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:7001`

#### Endpoints Principais & Documentação Interativa:
- **Interface Scalar UI (Documentação Viva):** [`https://localhost:7001/scalar/v1`](https://localhost:7001/scalar/v1) ou [`http://localhost:5000/scalar/v1`](http://localhost:5000/scalar/v1)
- **Contrato OpenAPI v1 (JSON):** `http://localhost:5000/openapi/v1.json`
- **Health Check Operacional:** `GET http://localhost:5000/api/v1/health`

---

### 5. Executar o Frontend (Blazor WebApp)

Em outro terminal, inicie a aplicação Blazor:

```bash
dotnet run --project src/Frontend/WebApp/WebApp.csproj
```

O portal estará disponível em:
- **HTTP:** `http://localhost:5130`
- **HTTPS:** `https://localhost:7285`

#### Rotas Principais do Frontend:
- **Dashboard Unificado de Performance:** `/`
- **Backoffice — Diretório 360° de Inquilinos:** `/admin/tenants` (busca, status, Shadow Mode/Impersonação, provisionamento)
- **Backoffice — Gestão de Planos & Cotas:** `/admin/plans` (planos comerciais, limites de gastos, assentos, features)
- **Backoffice — Monitor de Rate Limits & Saúde de APIs:** `/admin/api-health` (Meta, Google, Bing, TikTok, limiares de 80%)
- **Backoffice — Feature Flags & Kill Switches Operacionais:** `/admin/feature-flags` (disjuntores de emergência e rollouts)

---

## 🧪 Executando os Testes Automatizados (TDD)

O projeto segue rigorosamente a metodologia TDD e conta com **100% de testes verdes**.

### Executar Toda a Suíte de Testes (430+ testes)

> ⚠️ **Atenção:** Certifique-se de que o Docker está em execução, pois a suíte de testes de integração utiliza o **Testcontainers** para subir uma instância efêmera e isolada do SQL Server automaticamente.

```bash
dotnet test
```

### Executar Testes por Categoria

- **Testes Unitários do Backend:**
  ```bash
  dotnet test tests/UnitTests/Backend/UnitTests.Backend.csproj
  ```
- **Testes Unitários do Frontend (bUnit):**
  ```bash
  dotnet test tests/UnitTests/Frontend/UnitTests.Frontend.csproj
  ```
- **Testes de Integração com Banco Real (Testcontainers MSSQL):**
  ```bash
  dotnet test tests/IntegrationTests/IntegrationTests.csproj
  ```
- **Testes de Aceitação da WebApi (`WebApplicationFactory`):**
  ```bash
  dotnet test tests/AcceptanceTests/AcceptanceTests.csproj
  ```

---

## 📂 Estrutura do Repositório

```text
.
├── docker-compose.yml              # Setup rápido do SQL Server local
├── AGENTS.md                       # Regras arquiteturais mandatárias e convenções de código
├── docs/                           # Documentação viva e especificações do sistema
│   ├── adr/                        # Architecture Decision Records (ADRs em formato Nygard)
│   ├── modules/                    # Especificação funcional detalhada de cada subsistema
│   └── roadmaps/                   # Planos de conformidade e entregas
├── src/
│   ├── Backend/
│   │   ├── BuildingBlocks/         # Kernel compartilhado (Result, Entity base, Interfaces globais)
│   │   ├── Modules/
│   │   │   └── Master/             # Catálogo central: Inquilinos, Planos, Dunning, Flags, Audit
│   │   └── WebApi/                 # Host ASP.NET Core 10, OpenAPI, Scalar UI, Controllers
│   └── Frontend/
│       └── WebApp/                 # Blazor Server 10 (Components, Pages, State, White-Label)
└── tests/
    ├── UnitTests/                  # Testes unitários (Backend e Frontend via bUnit)
    ├── IntegrationTests/           # Testes de persistência com Testcontainers MSSQL
    └── AcceptanceTests/            # Testes de ponta a ponta com WebApplicationFactory
```

---

## 📜 Convenções de Código & Diretrizes

Antes de contribuir ou submeter alterações, consulte o arquivo [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md):
1. **Padronização `Result<T>`:** É expressamente proibido lançar exceções para fluxos de negócio ou validações. Toda operação retorna `Result` ou `Result<T>`.
2. **Documentação XML Mandatória:** Todo método, classe, interface, record e DTO público deve conter documentação XML completa (`<summary>`, `<param>`, `<returns>`).
3. **Isolamento de Tenants:** O `MasterDbContext` lida exclusivamente com o catálogo central; cada tenant opera isolado com seu respectivo `TenantDbContext`.
4. **TDD:** Escreva sempre o teste com falha (Red) antes de introduzir qualquer código de produção (Green & Refactor).

---

## 📄 Licença

Este projeto é de propriedade privada e confidencial. Todos os direitos reservados.
