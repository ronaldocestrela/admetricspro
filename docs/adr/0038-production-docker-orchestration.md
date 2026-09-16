# 0038. Orquestração de Produção com Docker Compose, Multi-Stage .NET 10 e Reverse Proxy Nginx

## Status
Aceito

## Contexto
O **AdMetricsPro** é composto por múltiplos serviços interdependentes:
1. Banco de dados relacional central (`MasterCatalog`) e bancos dedicados por inquilino (`TenantDb`) no SQL Server 2022.
2. Backend em Monólito Modular ASP.NET Core (.NET 10) responsável pela lógica de negócio, governança, segurança, automações e APIs de mídia.
3. Portal do Cliente / Inquilinos (`WebApp`) executado em **Blazor Server Interactive** (.NET 10).
4. Portal Administrativo Global (`BackofficeApp`) executado em **Blazor Server Interactive** (.NET 10).
5. Gateway de entrada / terminação com suporte a WebSockets/SignalR e roteamento seguro de domínios e subdomínios.

Os desafios identificados para ambiente produtivo incluíam:
- **Builds Reproduzíveis e Seguros:** Necessidade de compilação em múltiplos estágios (multi-stage) utilizando imagens oficiais do .NET 10, com usuário sem privilégios de root (`USER app`) para mitigação de vulnerabilidades de container breakout.
- **Circuitos Blazor Server & WebSockets:** Aplicações Blazor Server dependem de conexões persistentes SignalR. Timeouts padrão de proxies reversos derrubam os circuitos de tela dos usuários, causando desconexões frequentes e perda de estado de navegação.
- **Ordem Estrita de Inicialização & Migrações:** O backend `WebApi` só pode iniciar sua execução após o SQL Server estar efetivamente pronto para receber conexões transacionais (`service_healthy`). Além disso, as migrações automáticas do `MasterCatalog` devem ser aplicadas no boot antes de receber requisições de usuários.
- **Isolamento de Rede:** Serviços transacionais internos (SQL Server e endpoints internos da WebApi) não devem ficar expostos indiscriminadamente à internet pública.

## Decisão
Decidimos estruturar a orquestração de produção através de **Docker Compose** e **Dockerfiles Multi-Stage** dedicados para o ecossistema .NET 10:

1. **Dockerfiles Multi-Stage Otimizados (.NET 10):**
   - **Estágio Build:** Imagem base `mcr.microsoft.com/dotnet/sdk:10.0`. Copia primeiramente as configurações globais (`Directory.Build.props`) e os arquivos `.csproj` para restaurar pacotes NuGet (`dotnet restore`) aproveitando o cache de camadas do Docker. Em seguida, copia o código e compila via `dotnet publish -c Release --no-restore`.
   - **Estágio Runtime:** Imagem base `mcr.microsoft.com/dotnet/aspnet:10.0`. Instalação enxuta de utilitário de healthcheck (`curl`), execução sob usuário não-privilegiado `USER app` e binding na porta `8080` via `ASPNETCORE_HTTP_PORTS=8080`.

2. **Sincronização de Inicialização via Health Checks Nativos:**
   - Adicionamos o endpoint nativo `/health` no pipeline do ASP.NET Core `WebApi`.
   - O `sqlserver` executa healthcheck contínuo via `sqlcmd -Q "SELECT 1"`.
   - O serviço `webapi` depende formalmente de `sqlserver: service_healthy`.
   - As aplicações Blazor Server (`webapp` e `backoffice`) dependem de `webapi: service_healthy`.

3. **Reverse Proxy Nginx Especializado em WebSockets Blazor Server:**
   - Configuração de `map $http_upgrade $connection_upgrade` com headers `Upgrade` e `Connection`.
   - Timeouts estendidos (`proxy_read_timeout 86400s; proxy_send_timeout 86400s;`) e buffering desabilitado (`proxy_buffering off;`) para os circuitos Blazor Server.
   - Roteamento por Host: `app.*` para o portal de clientes, `admin.*` / `backoffice.*` para o portal administrativo e `api.*` para endpoints diretos da WebApi.

4. **Isolamento e Segurança de Variáveis de Ambiente:**
   - Disponibilização do template `.env.prod.example` contendo todas as variáveis essenciais (chaves HMAC-SHA256 para tokens JWT de impersonação e inquilinos, senhas do SA, flags de migração automática e credenciais do SuperAdmin inicial).
   - Portas transacionais de banco e serviços internos limitadas a `127.0.0.1` ou à rede interna virtual `admetricspro-prod-net`.

## Consequências

### Positivas
- **Deploy Simplificado com Um Único Comando:** Execução completa de toda a infraestrutura com `docker compose -f docker-compose.prod.yml up -d --build`.
- **Estabilidade de Conexão Blazor:** Eliminação de desconexões de tela e circuitos inválidos graças à configuração dedicada de WebSockets e timeouts no proxy reverso.
- **Segurança Reforçada:** Aplicação de princípio de menor privilégio (usuário `app`), portas de banco não expostas na internet pública e isolamento em redes virtuais Docker.
- **Zero-Downtime em Inicializações:** Garantia de que a API só atende requisições após o banco estar pronto e migrado.

### Negativas / Mitigações
- **Consumo de Memória do SQL Server em Container:**
  - *Mitigação:* O SQL Server 2022 em Linux gerencia eficientemente o pool de memória e pode ser restringido por diretivas `deploy.resources.limits.memory` caso necessário no ambiente do host.
