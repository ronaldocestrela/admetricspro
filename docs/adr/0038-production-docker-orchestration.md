# 0038. Orquestração de Produção com Docker Compose e Multi-Stage .NET 10

## Status
Aceito

## Contexto
O **AdMetricsPro** é composto por múltiplos serviços interdependentes:
1. Banco de dados relacional central (`MasterCatalog`) e bancos dedicados por inquilino (`TenantDb`) no SQL Server 2022.
2. Backend em Monólito Modular ASP.NET Core (.NET 10) responsável pela lógica de negócio, governança, segurança, automações e APIs de mídia.
3. Portal do Cliente / Inquilinos (`WebApp`) executado em **Blazor Server Interactive** (.NET 10).
4. Portal Administrativo Global (`BackofficeApp`) executado em **Blazor Server Interactive** (.NET 10).

Os desafios identificados para ambiente produtivo incluíam:
- **Builds Reproduzíveis e Seguros:** Necessidade de compilação em múltiplos estágios (multi-stage) utilizando imagens oficiais do .NET 10, com usuário sem privilégios de root (`USER app`) para mitigação de vulnerabilidades de container breakout.
- **Ordem Estrita de Inicialização & Migrações:** O backend `WebApi` só pode iniciar sua execução após o SQL Server estar efetivamente pronto para receber conexões transacionais (`service_healthy`). Além disso, as migrações automáticas do `MasterCatalog` devem ser aplicadas no boot antes de receber requisições de usuários.
- **Isolamento de Rede:** O SQL Server não deve ficar exposto na internet pública. Web API e portais Blazor precisam ser alcançáveis pelo IP ou domínio do servidor. TLS nas portas 80/443 fica fora da stack.

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

3. **Sem Proxy Reverso na Stack:**
   - O Compose de produção não inclui Nginx nem outro proxy.
   - `webapp`, `backoffice` e `webapi` publicam as portas `7000`, `7002` e `7001` em todas as interfaces do host.
   - `sqlserver` publica a porta somente em `127.0.0.1`.

4. **Isolamento e Segurança de Variáveis de Ambiente:**
   - Disponibilização do template `.env.prod.example` contendo todas as variáveis essenciais (chaves HMAC-SHA256 para tokens JWT de impersonação e inquilinos, senhas do SA, flags de migração automática e credenciais do SuperAdmin inicial).
   - A porta do SQL Server fica limitada a `127.0.0.1`. A comunicação entre containers usa a rede virtual `admetricspro-prod-net`.

## Consequências

### Positivas
- **Deploy Simplificado com Um Único Comando:** Execução completa de toda a infraestrutura com `docker compose -f docker-compose.prod.yml up -d --build`.
- **Segurança Reforçada:** Aplicação de princípio de menor privilégio (usuário `app`), SQL Server publicado apenas em `127.0.0.1` e isolamento da rede interna em `admetricspro-prod-net`.
- **Zero-Downtime em Inicializações:** Garantia de que a API só atende requisições após o banco estar pronto e migrado.

### Negativas / Mitigações
- **Consumo de Memória do SQL Server em Container:**
  - *Mitigação:* O SQL Server 2022 em Linux gerencia eficientemente o pool de memória e pode ser restringido por diretivas `deploy.resources.limits.memory` caso necessário no ambiente do host.
