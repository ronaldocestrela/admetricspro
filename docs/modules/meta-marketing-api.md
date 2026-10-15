# Integração Meta Marketing API

## Diagnóstico inicial — 15/10/2026

- Backend ASP.NET Core .NET 10, frontend Blazor Server, EF Core/SQL Server, MediatR e Result. Banco dedicado por tenant; workspaces são clientes dentro desse banco.
- OAuth compartilhado e adaptador Meta já existem. Não criar um segundo OAuth.
- State assinado tem TTL, mas faltam nonce, vínculo ao usuário e consumo persistente de uso único. O callback não compara tenant e redirect URI com o contexto atual.
- O host emite JWT de tenant, mas não registra sua validação no pipeline HTTP. Clientes HTTP de integração ainda não enviam Bearer.
- Cofre cifrado e único por workspace/plataforma já existe. O ID retornado por `/me` representa o autorizador, não uma conta de anúncios.
- ConnectedAdAccount já suporta várias contas por workspace; falta vínculo explícito com o cofre, descoberta paginada, seleção validada e desvinculação.
- Adaptadores de leitura existem, mas fixam v21.0 e a hierarquia busca somente a primeira página. Sincronização procura credencial por workspace/plataforma.
- Existem comandos de mutação de campanhas; sua execução externa, autorização e consistência precisam ser auditadas antes de considerar gerenciamento pronto.
- A autorização por carteira existe, mas depende de uso explícito e não protege uniformemente as operações de integração.

## Sequência de alterações

1. Testes de regressão e correção do OAuth: identidade validada, workspace autorizado, state aleatório e de uso único, callback contextual, configurações seguras e erros sem credenciais.
2. Descoberta paginada das contas, seleção confirmada na Meta, vínculo idempotente com a conexão e desvinculação local.
3. Sincronização apenas de vínculos ativos, versão configurável, paginação e métricas semanticamente corretas.
4. Gerenciamento externo de campanhas, conjuntos, anúncios e criativos, com testes HTTP simulados e sem publicar/ativar anúncios reais.
5. Interface, documentação operacional e verificação de isolamento.

## Fontes oficiais

As páginas developers.facebook.com retornaram indisponibilidade/429 na pesquisa inicial. O SDK oficial da Meta é uma fonte primária complementar, sem adicionar essa biblioteca ao projeto:

- [Versão v26.0 no SDK oficial](https://github.com/facebook/facebook-python-business-sdk/blob/26.0.0/facebook_business/apiconfig.py).
- [Contas acessíveis pelo usuário](https://github.com/facebook/facebook-python-business-sdk/blob/26.0.0/facebook_business/adobjects/user.py).
- [Campos da conta de anúncios](https://github.com/facebook/facebook-python-business-sdk/blob/26.0.0/facebook_business/adobjects/adaccount.py).
- [Prova de aplicativo em requisições](https://github.com/facebook/facebook-python-business-sdk/blob/26.0.0/facebook_business/session.py).

## Dependências externas

OAuth real depende do App ID, App Secret, callback cadastrado, chaves de assinatura/criptografia e conta autorizada. Aprovação de acesso avançado/App Review e autorização da Sandbox são externas. Nenhum teste automatizado deve usar token pessoal do desenvolvedor ou ativar publicidade real.

## Configuração operacional

Usar o carregamento de configuração/.env já existente. As chaves abaixo pertencem **ao backend**; não publicar esses valores em configuração estática do frontend:

| Chave | Finalidade |
|---|---|
| `META_APP_ID` | Aplicativo ADMetricsPro único para todos os clientes |
| `META_APP_SECRET` | Segredo do aplicativo, somente backend |
| `META_GRAPH_API_VERSION` | `v26.0`, confirmada no SDK oficial versionado |
| `META_OAUTH_REDIRECT_URI` | URL pública exata da página `/integrations`, cadastrada na Meta |
| `Integrations__OAuth__StateSigningKey` | Chave aleatória persistente de pelo menos 32 caracteres |
| `Integrations__OAuth__TokenEncryptionKey` | Chave aleatória AES-256 de 32 bytes, codificada em Base64 |
| `Integrations__OAuth__Meta__TimeoutSeconds` | Timeout de cada chamada externa, padrão 60 segundos |
| `Integrations__MetaSynchronization__Enabled` | Habilita leitura periódica, padrão `false` |
| `Integrations__MetaSynchronization__IntervalMinutes` | Intervalo entre ciclos, mínimo 5, padrão 60 |
| `Integrations__MetaSynchronization__LookbackDays` | Dias recentes reprocessados, de 1 a 90, padrão 7 |

Os nomes hierárquicos `Integrations:OAuth:Meta:AppId`, `AppSecret`, `GraphApiVersion` e `RedirectUri` também são aceitos; as variáveis `META_*` têm precedência quando presentes. Configuração parcial da Meta falha na validação de startup. Não usar as chaves de exemplo de desenvolvimento em produção. Todas as réplicas devem compartilhar as chaves persistentes, distribuídas pelo mecanismo de segredos do ambiente.

Para white-label, callbacks adicionais podem ser declarados em `Integrations:OAuth:Meta:AdditionalRedirectUris` e cadastrados também no aplicativo Meta. A URI precisa corresponder exatamente a um endereço configurado; subdomínios recebidos do navegador não são autorizados automaticamente.

Na inspeção local desta execução, App ID, App Secret, callback e chaves de assinatura/criptografia não estavam configurados nos nomes de ambiente suportados, incluindo aliases hierárquicos. Nenhuma credencial foi impressa ou utilizada em chamadas reais.

O backend valida os JWTs já emitidos por `TenantJwtTokenService` utilizando `TenantJwt:SecretKey`, issuer, audience, algoritmo e validade. As rotas de integração exigem Bearer antes do model binding. O tenant do JWT precisa corresponder ao tenant resolvido no pipeline HTTP.

A proteção também cobre consumidores dos dados sincronizados: dashboard executivo, Creative Hub, Copiloto, pacing, automações e relatórios privados. Listagem HTTP de workspaces usa a carteira do usuário do JWT, sem aceitar outro usuário por parâmetro. Consultas consolidadas sem workspace exigem permissão administrativa; relatórios públicos mantêm seu fluxo específico por token de compartilhamento. Rotas de cálculo puro que recebem todos os dados no payload não são convertidas em consultas a dados privados.

## OAuth e seleção por workspace

1. Um usuário autorizado administra a integração de um workspace específico.
2. A rota compartilhada `authorize-url` recebe `workspaceId`, `platform=MetaAds` e `redirectUri`.
3. O state assinado inclui tenant, usuário, workspace, plataforma, URI exata, timestamp e nonce aleatório. Somente o hash do state é persistido no banco do tenant, com TTL de 15 minutos.
4. A Meta retorna à página `/integrations`. Após restaurar a sessão Blazor, a página encaminha `code/state` ao callback existente da Web API, utilizando o JWT do usuário.
5. O callback compara sessão/tenant/URI com o state e o consome atomicamente antes da troca de código. Não recebe um workspace arbitrário para gravar a conexão. Estado reutilizado, expirado, adulterado ou de outra sessão é recusado.
6. O adaptador obtém o token de longa duração, consulta `debug_token`, confirma `is_valid`, aplicativo, usuário e escopos efetivamente concedidos; `/me` confirma a identidade autorizadora.
7. A credencial fica em `AwaitingAccountSelection`. `ExternalAccountId` no cofre identifica **o usuário autorizador**, não uma conta de anúncios.
8. O usuário consulta `/me/adaccounts` através do backend e seleciona contas. O backend verifica novamente a listagem autorizada e persiste os vínculos antes de ativar a conexão.

A autorização pede inicialmente `ads_read,ads_management`. `business_management` não é solicitado por padrão. Escopos concedidos parcialmente permitem leitura se `ads_read` estiver presente; escritas exigem `ads_management` e tarefas `ADVERTISE` ou `MANAGE` na conta. Os papéis internos e a carteira do usuário também são verificados. Administrar credenciais/vínculos exige `ManageWorkspaces`; editar publicidade exige `EditCampaigns`; consultar dados exige `ViewCampaigns`.

Novos tokens Meta usam AES-256-GCM com nonce aleatório e dados autenticados que incluem tenant, workspace, plataforma e ID da conexão. Copiar o ciphertext para outra conexão ou tenant não permite descriptografá-lo. Credenciais Meta legadas exigem reconexão; a leitura legada permanece disponível para os outros provedores existentes. Tokens nunca fazem parte dos DTOs de resposta.

Uma conexão pode autorizar várias contas no mesmo workspace. O mesmo ID externo pode existir em workspaces diferentes quando autorizado legitimamente em ambos, com dados e credenciais separados. A unicidade é `(WorkspaceId, Platform, ExternalAccountId)` e a FK do vínculo inclui `WorkspaceId` e `OAuthTokenVaultId`.

Reconexão exige nova seleção, invalidando os vínculos locais anteriores. Desvincular conta ou desconectar workspace preserva o histórico. Desconexão Meta **não** chama automaticamente `DELETE /me/permissions`, pois essa revogação afeta a concessão do aplicativo e pode atingir outros workspaces do mesmo usuário.

Tokens de usuário Meta não têm refresh token para renovação indefinida. O botão compartilhado de renovação preserva a credencial Meta ainda válida e marca como expirada quando o prazo conhecido vence. Revogação detectada em consultas/escritas exige reconexão.

## Endpoints

Todas as rotas estão em `/api/v1/integrations`, com OpenAPI/Scalar no padrão existente. `accountId` representa sempre o **ID interno do vínculo**, salvo onde `externalId` é explicitamente indicado.

| Método e sufixo | Operação |
|---|---|
| `GET oauth/authorize-url` | OAuth compartilhado existente |
| `GET oauth/callback` | Callback compartilhado, com `code/state/redirectUri` ou negativa `error` |
| `GET oauth/status/{workspaceId}` | Estado da credencial, sem tokens |
| `DELETE oauth/{workspaceId}/MetaAds` | Desconexão local do workspace |
| `GET meta/ad-accounts?workspaceId=...` | Contas acessíveis pela autorização |
| `POST meta/ad-accounts/select` | Verificação e vínculo idempotente de contas |
| `DELETE meta/ad-accounts/{accountId}?workspaceId=...` | Desvínculo local de uma conta |
| `GET connected-accounts?workspaceId=...` | Contas já vinculadas, rota existente |
| `POST campaigns/sync` | Hierarquia compartilhada: campanhas, conjuntos e anúncios |
| `POST campaigns/metrics/sync` | Ingestão diária compartilhada |
| `GET campaigns?workspaceId=...` | Hierarquia persistida, rota existente |
| `GET meta/insights?workspaceId=...&accountId=...&since=...&until=...&level=...` | Insights detalhados |
| `GET meta/resources/{resource}/{externalId}?workspaceId=...&accountId=...` | Detalhes e status efetivo remoto |
| `POST meta/resources` | Criação ou edição explícita com idempotência |
| `GET meta/assets/{asset}?workspaceId=...&accountId=...` | Páginas, Instagram, pixels, públicos ou mídias elegíveis |
| `GET meta/targeting-search?workspaceId=...&accountId=...&type=...&query=...` | Interesses, idiomas ou países por busca oficial |
| `POST meta/media` | Upload multipart de PNG/JPEG ou MP4 |
| `GET meta/media/videos/{operationId}?workspaceId=...&accountId=...` | Processamento de vídeo enviado pelo workspace |
| `GET meta/operations/{operationId}?workspaceId=...&accountId=...` | Diário de operação sem reexecutar escrita |

Recursos aceitos: `campaigns`, `adsets`, `ads`, `adcreatives`. Ativos aceitos: `promote_pages`, `instagram_accounts`, `adspixels`, `customaudiences`, `adimages`, `advideos`. Busca: `adinterest`, `adlocale`, `adgeolocation` (países no fluxo atual).

### Seleção de conta

```json
{
  "workspaceId": "11111111-1111-1111-1111-111111111111",
  "accountIds": ["act_123456789"]
}
```

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": ["22222222-2222-2222-2222-222222222222"]
}
```

IDs não encontrados na listagem autorizada são recusados com `Meta.AccountNotAuthorized`. Uma conta não vinculada não pode ser usada para sincronização, leitura detalhada ou escrita.

### Criação de campanha pausada

```json
{
  "operationId": "33333333-3333-3333-3333-333333333333",
  "workspaceId": "11111111-1111-1111-1111-111111111111",
  "accountId": "22222222-2222-2222-2222-222222222222",
  "resource": "campaigns",
  "externalId": null,
  "confirmActivation": false,
  "data": {
    "name": "Campanha de tráfego",
    "objective": "OUTCOME_TRAFFIC",
    "special_ad_categories": [],
    "status": "PAUSED"
  }
}
```

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": { "externalId": "1234567890", "success": true }
}
```

Para edição, informar `externalId` e somente os campos desejados. `status` aceita `PAUSED`, `ACTIVE`, `ARCHIVED`, `DELETED`, quando suportados pelo recurso. Ativação exige `confirmActivation=true`; criação publicitária é sempre pausada. Os objetivos modernos `OUTCOME_*` são validados, mas elegibilidade depende da conta e da versão da Meta. Orçamento diário e vitalício não são enviados juntos. Valores da API são inteiros nas unidades monetárias mínimas exigidas pela Meta; a interface monetária inicial converte BRL/USD/EUR com precisão decimal, recusando conversão automática para outras moedas.

Conjuntos exigem campanha, `billing_event`, `optimization_goal` e `targeting`; podem informar orçamento, lances, datas, `promoted_object` e atribuição. Anúncios exigem conjunto e `creative.creative_id`. Criativos aceitam especificações oficiais de história/ativos e rastreamento; edição de criativo existente limita-se ao nome. Para mudar conteúdo, criar outro criativo e associá-lo ao anúncio.

O backend confirma a conta proprietária de objetos existentes e de pais/criativos antes de escrever. Páginas, Instagram, imagens e vídeos referenciados em novos criativos são verificados entre os ativos elegíveis da conta. Vídeos ainda em processamento não são tratados como prontos.

### Idempotência e erros de escrita

Preservar `operationId` e o mesmo pedido ao consultar/repetir uma intenção de escrita. A reserva é gravada antes de enviar POST. Uma repetição de operação concluída devolve o ID confirmado; alteração do pedido com a mesma chave retorna conflito. `Pending` ou `Unknown` não provocam novo POST. Falha de rede/cancelamento após envio exige consultar a conta e reconciliar o resultado, pois não há garantia de rollback remoto.

O diário `MetaApiOperations` armazena usuário, workspace, conta, recurso, hash, estado, ID externo e timestamps; não armazena tokens ou o payload publicitário completo. As ações preexistentes de pausa, ativação e orçamento agora encaminham contas Meta reais ao mesmo fluxo externo antes de alterar o banco local. Contas demonstrativas preservam a simulação explícita. Realocação entre duas campanhas Meta reais é recusada até existir reconciliação de duas escritas; não é apresentada como uma transação remota atômica.

| Código | Tratamento |
|---|---|
| `OAuthState.AlreadyUsed`, `OAuthState.Expired` | Iniciar novo OAuth |
| `OAuth.ContextMismatch`, `Auth.TenantMismatch` | Recusar associação ou contexto indevido |
| `OAuth.Denied` | Consentimento negado, sem trocar código |
| `Meta.ConnectionUnavailable`, `Meta.TokenInvalid` | Reconectar e selecionar contas |
| `Meta.AccountNotAuthorized`, `Meta.AccountNotLinked` | Corrigir autorização/vínculo |
| `Meta.ResourceAccountMismatch`, `Meta.AssetNotAuthorized` | Recusar objeto/ativo de outra conta |
| `Meta.ScopeMissing`, `Meta.WriteForbidden`, `Meta.PermissionDenied` | Verificar permissões internas e externas |
| `Meta.InvalidWrite`, `Meta.WriteRejected` | Corrigir campos; mensagens de usuário da Meta são sanitizadas |
| `Meta.OperationConflict`, `Meta.OperationAlreadySubmitted` | Consultar operação, sem repetir POST |
| `Meta.WriteOutcomeUnknown` | Reconciliar na Meta antes de outra tentativa |
| `Meta.TransientFailure`, `Meta.Timeout` | Leituras têm backoff limitado; escrita não é repetida automaticamente |
| `Meta.InvalidMedia`, `Meta.MediaRejected`, `Meta.VideoNotReady` | Corrigir arquivo ou aguardar processamento |

## Métricas e sincronização

Hierarquia e Insights usam a versão configurada, Bearer no header e `appsecret_proof`. Paginação segue somente cursores em host fixo, sem enviar credenciais a `paging.next`. Leituras têm timeout, backoff e cancelamento. Falha de uma página impede apresentar uma coleção incompleta como sucesso. Upserts preservam IDs locais e o índice SQL Server também cobre métricas diárias com dimensões nulas.

A ingestão unificada persiste gasto, impressões e cliques por campanha/dia. Ela **não** soma `actions` como compras/conversões. Como o modelo unificado existente não define evento de conversão por conta, CPA/ROAS sem dados ficam nulos no contrato de consulta Meta; análises de conversão devem usar a consulta detalhada com evento explícito. Dashboards unificados legados precisam considerar essa limitação de conversões, sem interpretar ausência como prova de zero vendas.

Insights detalhados suportam `account`, `campaign`, `adset`, `ad`, datas inclusivas (até 366 dias), filtros por IDs de campanha/conjunto/anúncio e um breakdown por consulta: `publisher_platform`, `device_platform` ou `platform_position`. Não se presume combinação arbitrária de breakdowns. `conversionAction` escolhe exatamente uma chave de ação. CPA exige contagem desse evento; ROAS exige seu valor de conversão e gasto positivo. Cliques totais e cliques no link são separados. Campos ausentes ficam nulos; alcance/frequência não são somados. A consulta utiliza a configuração de atribuição unificada fornecida pela Meta.

A sincronização compartilhada Meta suporta granularidade diária. O antigo valor fictício `time_increment=hourly` é recusado; suporte horário demanda breakdown oficial e contrato próprio. Consultas assíncronas de Insights para grandes volumes ainda não foram implementadas; o fluxo atual é síncrono e paginado.

O worker opcional enumera tenants ativos/trial, workspaces ativos e contas reais conectadas. Cada conta recebe novo escopo e contexto de tenant antes da resolução das dependências. Reprocessa dias recentes no fuso da conta, registra execução no diário e logs estruturados sem credenciais, e não publica nem altera anúncios. Ciclos são sequenciais na instância. Uma instalação com múltiplas réplicas deve habilitar o worker em uma só réplica até haver lease distribuído; unicidade de dados não equivale a coordenação distribuída de execução.

## Interface

- `/integrations`: autorização por cliente, seleção de várias contas e desvinculação local.
- `/workspaces/{workspaceId}/meta`: conta ativa, sincronização, pesquisa e status de campanhas, navegação para conjuntos/anúncios, métricas, formulários e confirmação de ativação.
- Novos recursos publicitários são pausados; o status configurado é distinguido do status efetivo consultado na Meta.
- O formulário de conjunto consulta as categorias especiais da campanha antes de oferecer idade/gênero. Interesses, idiomas e países vêm de pesquisa oficial; públicos e identidades vêm dos ativos autorizados.
- Posicionamento visual inicial: automático ou feed Facebook. Outros posicionamentos oficiais podem ser enviados pelo contrato de API, sujeitos às restrições da Meta.
- Upload de PNG/JPEG até 10 MB e MP4 até 100 MB. Vídeos têm consulta de processamento. Criativos contemplam imagem, vídeo e carrossel; aprovações/reprovações dependem da revisão Meta.
- Todas as chamadas usam clientes HTTP tipados; não foi adicionado acesso a banco no frontend. Respostas de operações iniciadas em outro contexto são descartadas ao trocar workspace/conta.

## Migrações e implantação

Foram geradas migrations operacionais para reconciliação do schema preexistente, states/vínculos, diário de operações e unicidade de métricas diárias. Novos tenants utilizam o provisionamento já existente. Tenants existentes precisam receber as migrations operacionais durante a implantação pelo procedimento de atualização do ambiente. Não confundir migrations do catálogo Master com as migrations dos bancos dos tenants.

Nenhuma migration foi aplicada a bancos reais nesta implementação. Bancos criados anteriormente via `EnsureCreated` ou com schema manual precisam ser comparados com seu histórico antes da atualização. A reconciliação separada versiona tabelas que o código já usava e cujo snapshot anterior estava incompleto; revisar esse pré-requisito em homologação com backup.

Proxies e infraestrutura de observabilidade também precisam omitir queries de callback e headers Authorization. Os clientes HTTP Meta e os logs padrão de início de requisição do host foram configurados para não registrar esses parâmetros sensíveis.

## Testes e validação externa

Testes automatizados usam HTTP simulado e SQLite/metadata SQL Server. Cobrem estado adulterado/expirado/reutilizado, usuário/tenant/URI divergentes, JWT inválido, vínculo de outra conexão, troca de ciphertext, paginação, precisão decimal, CPA/ROAS explícitos, idempotência de POST, propriedade de recursos, arquivos, jobs e componentes Blazor. Os testes não utilizam tokens pessoais nem ativam publicidade real.

Executar:

```sh
dotnet test tests/UnitTests/Backend/UnitTests.Backend.csproj --no-restore -m:1
dotnet test tests/UnitTests/Frontend/UnitTests.Frontend.csproj --no-restore -m:1
dotnet test tests/AcceptanceTests/AcceptanceTests.csproj --no-restore -m:1
dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantProvisioningServiceTests -m:1
```

A última linha depende do Docker/Testcontainers e SQL Server. No ambiente desta implementação, o Docker estava indisponível, impedindo validar o provisionamento em SQL Server. DDL e migrações podem ser inspecionados sem conexão, mas isso não substitui a aplicação real em homologação.

Antes de produção, validar com conta expressamente autorizada: OAuth pelo navegador, tokens persistidos, seleção, leitura/sincronização, uploads e criação/edição pausada. Não executar ativação real sem autorização específica. A Sandbox que retornou `(#200)` continua como pendência de autorização/configuração; leituras reais previamente confirmadas pelo responsável não são evidência de autorização de escrita na Sandbox.

App Review/acesso avançado para contas de terceiros, eventuais verificações de negócio, permissões adicionais para páginas/Instagram/pixels e elegibilidade de categorias/objetivos dependem da Meta e dos ativos. `ads_management` não concede automaticamente acesso a todos os ativos. Não há automação de aprovação, catálogo fictício de comportamentos, criação de públicos com dados pessoais, promessa de paridade integral com Ads Manager ou suporte a todo formato/combinação disponível na interface da Meta.
