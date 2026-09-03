# ADR 0018: Gestão de Segredos e Configurações Sensíveis via Arquivo .env

## Status
Aceito

## Contexto
O ecossistema **AdMetricsPro** opera com múltiplas informações sensíveis e credenciais de infraestrutura:
1. Cadeias de conexão com senhas do banco de catálogo central `MasterDb` em SQL Server.
2. Chaves criptográficas simétricas (HMAC-SHA256 de 256 bits) para assinatura e validação de tokens JWT de *Shadow Mode* (impersonação de inquilinos).
3. Senhas de administração de contêineres Docker (`MSSQL_SA_PASSWORD`).
4. URLs e credenciais de comunicação entre serviços e módulos.

O armazenamento dessas credenciais diretamente em arquivos rastreados pelo controle de versão (como `appsettings.json`) acarreta sérios riscos de vazamento e quebra de conformidade de segurança. Além disso, diferentes desenvolvedores e ambientes (desenvolvimento local, Docker, CI/CD, staging) exigem parametrizações dinâmicas e isoladas sem impactar a base de código versionada.

## Decisão

1. **Adoção do Padrão `.env` com Template `.env.example`:**
   - Adicionamos o arquivo `.env` e variações `.env.*` ao `.gitignore`, assegurando que segredos locais nunca sejam comitados.
   - Fornecemos um template público e versionado [`.env.example`](file:///home/rony/LPR/AdMetricsPro/.env.example) contendo todas as variáveis necessárias, com descrições didáticas e valores padrão seguros para desenvolvimento local.

2. **Componente Desacoplado no Kernel Compartilhado (`BuildingBlocks.Infrastructure`):**
   - Implementamos a classe utilitária `DotEnvLoader`:
     - Varredura ascendente e recursiva: localiza o arquivo `.env` a partir do diretório de execução atual subindo até a raiz do repositório, garantindo funcionamento idêntico se a aplicação for executada a partir da raiz da solução ou da pasta interna de cada projeto.
     - Parsing robusto com suporte a comentários `#`, linhas em branco e remoção de delimitadores de aspas duplas ou simples.
     - Não bloqueante (`optional = true` por padrão), permitindo execução perfeita em ambientes conteinerizados ou de nuvem (onde as variáveis já são injetadas no ambiente do sistema operacional).
   - Implementamos a extensão fluente `IConfigurationBuilder.AddDotEnvFile()`:
     - Injeta as variáveis do `.env` na árvore de configuração do ASP.NET Core e normaliza chaves hierárquicas .NET (convertendo automaticamente `Seção__Chave` para `Seção:Chave`).

3. **Inicialização Integrada no Backend e Frontend:**
   - O `src/Backend/WebApi/Program.cs` e o `src/Frontend/WebApp/Program.cs` invocam `DotEnvLoader.Load()` e `builder.Configuration.AddDotEnvFile()` antes da resolução de serviços e conexões.

4. **Integração com Docker Compose:**
   - O arquivo `docker-compose.yml` consome as variáveis `${MSSQL_SA_PASSWORD}` e `${MSSQL_PORT}` declaradas no `.env`, unificando a experiência local de desenvolvimento.

## Consequências

### Positivas
- **Segurança de Credenciais:** Nenhuma senha ou chave JWT sensível precisa ser mantida nos arquivos de configuração versionados no Git.
- **Onboarding de Desenvolvedores em 1 Passo:** Novos desenvolvedores executam apenas `cp .env.example .env` e estão prontos para rodar localmente.
- **Transparência Multi-Ambiente:** O mesmo mecanismo atende perfeitamente à execução direta via CLI (`dotnet run`), IDEs (VS Code / Visual Studio) e Docker Compose.
- **Zero Dependências Externas Frágeis:** Implementado diretamente sobre a biblioteca padrão do .NET e `Microsoft.Extensions.Configuration`, garantindo conformidade estrita com o .NET 10 e zero vulnerabilidades de terceiros.

### Negativas / Mitigações
- Exige que o desenvolvedor crie o `.env` ou confie nos valores de fallback locais. Para mitigar, a documentação viva no `README.md` e o script de execução explicam detalhadamente o passo a passo.
