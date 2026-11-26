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
| **`rabbitmq`** | `rabbitmq:4` | `5672` (AMQP) | **Nenhuma** | Broker de mensageria; tráfego somente na rede `admetricspro-prod-net`. Não exponha `5672` nem `15672` na internet. |
| **`minio`** | `minio/minio` | `9000` (API S3), `9001` (console) | **Nenhuma** | Object storage S3-compatível; tráfego somente na rede `admetricspro-prod-net`. Não exponha `9000` nem `9001` na internet. |
| **`minio-init`** | `minio/mc` | — | — | Job único: cria buckets privados e regra ILM de staging; idempotente. |
| **`webapi`** | `src/Backend/WebApi/Dockerfile` | `8080` | `7001` | Backend Monólito Modular ASP.NET Core .NET 10. |
| **`webapp`** | `src/Frontend/WebApp/Dockerfile` | `8080` | `7000` | Painel do Cliente / Inquilinos (Blazor Server). |
| **`backoffice`** | `src/Frontend/BackofficeApp/Dockerfile` | `8080` | `7002` | Painel do Administrador Global (Blazor Server). |

---

## 4. Ordem de Inicialização e Health Checks

1. **SQL Server, RabbitMQ e MinIO iniciam em paralelo:** Health checks nativos (`sqlcmd`, `rabbitmq-diagnostics -q ping`, `GET /minio/health/live` no container `minio`).
2. **`minio-init` após MinIO saudável:** Cria os três buckets se não existirem e aplica expiração no prefixo de staging orgânico. Reexecutar é seguro: `docker compose --env-file .env.prod -f docker-compose.prod.yml run --rm minio-init`.
3. **WebApi aguarda SQL Server e RabbitMQ:** O container `webapi` possui `depends_on` com `service_healthy` em `sqlserver` e `rabbitmq` (não aguarda MinIO enquanto `ObjectStorage__Provider=Local`). Ao iniciar, executa as migrações automáticas do banco `MasterCatalog` quando configurado (`DatabaseMigrations__ApplyMasterMigrationsOnStartup=true`).
4. **WebApps aguardam a WebApi:** Os portais Blazor Server (`webapp` e `backoffice`) iniciam somente após a `webapi` reportar status saudável em `GET /health`.

### RabbitMQ em produção (Fase 1)

No arquivo `.env.prod` do operador, defina credenciais fortes (nunca versionadas):

- `RABBITMQ_DEFAULT_USER`, `RABBITMQ_DEFAULT_PASS`, `RABBITMQ_DEFAULT_VHOST` (padrão `admetricspro`)
- `Messaging__Enabled=false` mantém o comportamento legado: o health check AMQP não é registrado e `/health` permanece 200 mesmo se o broker cair depois.
- Com `Messaging__Enabled=true`, a Web API exige broker acessível em `rabbitmq:5672` na rede interna; falha de conexão deixa o check `rabbitmq` **Unhealthy** e o agregado `/health` retorna 503.

O plugin de management **não** é publicado em produção (`rabbitmq:4` sem UI). Diagnóstico operacional do broker ocorre via logs do container ou ferramentas na rede interna, não pela internet.

### MinIO em produção (Fase 1 object storage)

No `.env.prod` do operador, defina credenciais fortes (nunca versionadas):

- `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` — usuário dedicado; **não** use `minioadmin`.
- `ObjectStorage__Provider=Local` (padrão no Compose) mantém o comportamento legado: nenhum health check `minio`, nenhum socket S3 na API, `IObjectStore` em disco (`ObjectStorage__LocalRoot`, padrão `./data/object-storage`) e a mídia orgânica continua no volume `social-media-prod-data`.
- Com `ObjectStorage__Provider=MinIO`, a Web API registra o check nomeado `minio` em `GET /health`; cluster inacessível em `http://minio:9000` deixa o agregado **Unhealthy** (503). A **mídia orgânica** (Fase 4) grava no bucket `admetricspro-organic` quando `MinIO` está ativo; o padrão do repositório permanece **`ObjectStorage__Provider=Local`** até a Fase 5 concluir a cópia do volume `social-media-data` em ambientes legados. Overlay opcional: `docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.minio.yml up` (dev: `docker-compose.yml` + `docker-compose.minio.yml`). Com `ObjectStorage__PublicBaseUrl` definido, configure `ObjectStorage__PublishUrlSigningKey` (mín. 32 caracteres).
- Console MinIO (`9001`) e API S3 (`9000`) **não** são publicados no host. Diagnóstico via logs do container ou `mc` na rede interna.

Variáveis de referência (ADR 0018): `ObjectStorage__LocalRoot`, `ObjectStorage__ServiceUrl`, `ObjectStorage__OrganicBucket`, `ObjectStorage__ReportsBucket`, `ObjectStorage__BrandingBucket`, `ObjectStorage__Region`, `ObjectStorage__PublicBaseUrl`, `ObjectStorage__PublishUrlTtlMinutes`, `ObjectStorage__MigrateLocalRoot` (padrão `false`; **nunca** `true` nas réplicas que servem HTTP no dia a dia).

**Runbook — cópia do volume orgânico (Fase 5)**

1. Subir MinIO e `minio-init` (buckets privados). As réplicas que atendem tráfego permanecem com `ObjectStorage__Provider=Local` e o volume `social-media-prod-data` montado em `SocialPublishing__Media__LocalRoot`.
2. Disparar **um** processo da imagem `webapi` com overlay MinIO, volume legado montado, `ObjectStorage__Provider=MinIO`, `ObjectStorage__MigrateLocalRoot=true` e `Messaging__HostRole=Api` (evita laços de scheduler durante a cópia). Exemplo: `docker compose --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.minio.yml run --rm -e ObjectStorage__MigrateLocalRoot=true -e Messaging__HostRole=Api webapi`.
3. Conferir o log estruturado: `Copiados + Ignorados` deve refletir os arquivos do volume; `Falhos` = 0 antes do corte; revisar `AusentesNoDisco` (buraco SQL × disco).
4. Somente então alterar `ObjectStorage__Provider=MinIO` nas réplicas de produção. **Manter** o volume montado por uma janela de rollback. Remover o volume não faz parte desta fase.

### Mensageria multi-instância (Fase 10)

Com `Messaging__Enabled=true`, use `Messaging__HostRole` para separar responsabilidades:

| Papel | Variável | HTTP público | Broker no `/health` | Laços (Meta, orgânico, dunning, trial, dispatch) | Consumidores |
| :--- | :--- | :---: | :---: | :---: | :---: |
| Instância única (padrão) | `All` | Sim | Sim | Sim | Sim |
| API HTTP | `Api` | Sim | **Não** | Não | Não |
| Scheduler | `Scheduler` | Opcional | Sim | Sim (lease único) | Não |
| Worker | `Consumer` | Não | Sim | Não | Sim (N réplicas) |

**Runbook rápido**

1. **Broker fora:** réplicas `Api` continuam gravando outbox em SQL e respondendo HTTP; `Scheduler`/`Consumer` ficam Unhealthy em `/health` até o RabbitMQ voltar.
2. **Kill switch:** `killswitch.messaging.dispatch` no Backoffice congela publicação; linhas permanecem `Pending` no SQL.
3. **Reclaim:** linhas `InProgress` expiradas voltam a `Pending` via `Messaging:Dispatch:InProgressTimeoutSeconds` (dispatcher).
4. **Drenar fila sem perder dados:** pare consumidores (`Consumer`), resolva a causa; a fonte da verdade é a outbox SQL até `Published`. Não apague filas sem consultar dead-letters no Backoffice (`/messaging-dead-letters`).
5. **Dead-letter:** consulte `GET /api/v1/admin/messaging/dead-letters` ou o Backoffice; reprocessar cria nova linha `Pending` com novo `EventId` e `correlationId` apontando para o evento original.
6. **Métricas:** contadores `admetrics.messaging.dispatch`, `admetrics.messaging.acknowledged`, `admetrics.messaging.rescheduled`, `admetrics.messaging.dead_letter` (meter `AdMetricsPro.Messaging`).

Exemplo de réplicas em produção (mesma imagem `webapi`, variáveis diferentes):

```bash
# Edge HTTP (pode escalar)
Messaging__HostRole=Api

# Um scheduler com lease no MasterDb
Messaging__HostRole=Scheduler

# Consumidores horizontais
Messaging__HostRole=Consumer
```

O serviço `webapi` padrão no compose continua `Messaging__HostRole=All` para instalações de nó único.

---

## 5. Comandos Úteis de Operação e Monitoramento

### Falha de restore com NU1902/NU1903 no pacote PKCS

O driver `Microsoft.Data.SqlClient` usado pelo EF Core trazia a dependência
transitiva `System.Security.Cryptography.Pkcs` 9.0.11. Os avisos
[GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) e
[GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx)
identificam essa versão como vulnerável e indicam 10.0.13 como versão corrigida
na linha .NET 10.

`BuildingBlocks.Infrastructure` referencia explicitamente a versão 10.0.13,
que se propaga aos hosts WebApi, WebApp e BackofficeApp. A auditoria NuGet e
`TreatWarningsAsErrors` continuam habilitados. O teste
`PkcsSecurityDependencyTests` verifica a versão efetiva carregada pela aplicação.

Após atualizar os fontes, reconstrua as imagens com `docker compose up -d --build`.
Não é necessário remover volumes nem recriar os bancos.

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

## 6. Acesso aos Serviços

A stack de produção não inclui proxy reverso. WebApp, Web API e Backoffice escutam em todas as interfaces do host:

- Painel do Cliente (WebApp): porta `7000` (`WEBAPP_PORT`)
- Web API: porta `7001` (`WEBAPI_PORT`)
- Painel SuperAdmin (BackofficeApp): porta `7002` (`BACKOFFICE_PORT`)

O SQL Server permanece apenas em `127.0.0.1:1433` (`MSSQL_PORT`). Os containers da aplicação falam com ele pela rede interna `admetricspro-prod-net`.

Um domínio funciona quando o DNS aponta para o IP do servidor e o cliente usa a porta publicada, por exemplo `http://app.seudominio.com:7000`. TLS e redirecionamento das portas 80/443 ficam fora deste Compose.
