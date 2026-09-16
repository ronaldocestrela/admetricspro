# Guia Operacional de Deploy em Produção via Docker Compose

Este documento fornece as instruções operacionais para publicar, inicializar e monitorar a stack completa do **AdMetricsPro** em ambiente de produção utilizando Docker e Docker Compose.

---

## 1. Pré-Requisitos do Servidor

- **Sistema Operacional:** Linux (Ubuntu 22.04 LTS / 24.04 LTS / Debian 12 ou superior recomendado).
- **Docker Engine:** Versão 24.0 ou superior instalada.
- **Docker Compose:** Plugin v2 (`docker compose`) instalado.
- **Recursos Mínimos Recomendados:**
  - CPU: 4 vCPUs.
  - Memória RAM: Mínimo 8 GB (16 GB recomendado para comportar o SQL Server + compilação e múltiplos circuitos Blazor).
  - Disco: Mínimo 40 GB SSD com volume persistente.

---

## 2. Passo a Passo de Inicialização

### Passo 1: Clonar o Repositório e Preparar o Ambiente
No servidor de produção:
```bash
git clone https://github.com/ronaldocestrela/admetricspro.git /opt/admetricspro
cd /opt/admetricspro
```

### Passo 2: Criar o Arquivo de Variáveis de Produção
Copie o template de produção:
```bash
cp .env.prod.example .env.prod
```

Edite o arquivo `.env.prod` com seus valores reais e senhas seguras:
```bash
nano .env.prod
```

> [!CAUTION]
> **Atenção aos Itens Críticos de Segurança:**
> - Altere `MSSQL_SA_PASSWORD` para uma senha forte (mínimo 12 caracteres contendo letras maiúsculas, minúsculas, números e caracteres especiais).
> - Gere chaves criptográficas fortes de no mínimo 32 caracteres (256 bits) para `ImpersonationJwt__SecretKey` e `TenantJwt__SecretKey`.
> - Defina a senha do `SuperAdmin__Password` antes de subir o container pela primeira vez.

### Passo 3: Inicializar a Stack de Produção
Execute o comando de build e inicialização em segundo plano (detached):
```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

---

## 3. Topologia dos Containers e Redes

| Serviço | Imagem / Build | Porta Interna | Porta Exposta no Host | Função |
| :--- | :--- | :--- | :--- | :--- |
| **`sqlserver`** | `mssql/server:2022-latest` | `1433` | `127.0.0.1:1433` | Banco de dados `MasterCatalog` e bancos isolados de inquilinos. |
| **`webapi`** | `src/Backend/WebApi/Dockerfile` | `8080` | `127.0.0.1:7001` | Backend Monólito Modular ASP.NET Core .NET 10. |
| **`webapp`** | `src/Frontend/WebApp/Dockerfile` | `8080` | `127.0.0.1:7000` | Painel do Cliente / Inquilinos (Blazor Server). |
| **`backoffice`** | `src/Frontend/BackofficeApp/Dockerfile` | `8080` | `127.0.0.1:7002` | Painel do Administrador Global (Blazor Server). |
| **`proxy`** | `nginx:alpine` | `80`, `443` | `80`, `443` | Reverse Proxy, SSL e Upgrade de WebSockets. |

---

## 4. Ordem de Inicialização e Health Checks

1. **SQL Server Inicia Primeiro:** O container do banco de dados inicializa e executa checagens periódicas via `/opt/mssql-tools18/bin/sqlcmd`.
2. **WebApi Aguarda o SQL Server:** O container `webapi` possui `depends_on: sqlserver` com condição `service_healthy`. Ao iniciar, ele executa as migrações automáticas do banco `MasterCatalog` (`DatabaseMigrations__ApplyMasterMigrationsOnStartup=true`).
3. **WebApps Aguardam a WebApi:** Os portais Blazor Server (`webapp` e `backoffice`) iniciam somente após a `webapi` reportar status saudável em seu endpoint `/health`.
4. **Nginx Expõe o Tráfego:** O proxy reverso Nginx distribui o tráfego externo para os serviços correspondentes.

---

## 5. Comandos Úteis de Operação e Monitoramento

### Verificar o Status dos Containers
```bash
docker compose -f docker-compose.prod.yml ps
```

### Visualizar Logs em Tempo Real
```bash
# Todos os serviços:
docker compose -f docker-compose.prod.yml logs -f

# Apenas a Web API:
docker compose -f docker-compose.prod.yml logs -f webapi

# Apenas os painéis Blazor:
docker compose -f docker-compose.prod.yml logs -f webapp backoffice
```

### Parar e Reiniciar a Aplicação
```bash
# Parar com segurança
docker compose -f docker-compose.prod.yml stop

# Reiniciar
docker compose -f docker-compose.prod.yml start

# Recriar serviços após atualizações de código:
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

### Executar Backup do SQL Server
```bash
docker exec -it admetricspro-prod-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "SuaSenhaAqui" -C \
  -Q "BACKUP DATABASE [MasterCatalog] TO DISK = N'/var/opt/mssql/data/MasterCatalog_Backup.bak' WITH NOFORMAT, NOINIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10"
```

---

## 6. Configuração de Domínios e DNS

Para roteamento automático via Nginx com subdomínios, aponte os seguintes registros tipo `A` ou `CNAME` no seu provedor de DNS para o IP do seu servidor:

- `app.seudominio.com` ➔ Painel do Cliente (WebApp)
- `admin.seudominio.com` ou `backoffice.seudominio.com` ➔ Painel SuperAdmin (BackofficeApp)
- `api.seudominio.com` ➔ Endpoints REST / OpenAPI da Web API
- `*.seudominio.com` (Wildcard) ➔ Permite a resolução dinâmica de domínios personalizados e White-Label de agências.
