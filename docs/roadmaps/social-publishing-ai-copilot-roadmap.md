# Roadmap de Implementação: Gestão e Publicação Orgânica com IA (AI Social Copilot & Scheduling)

Roadmap técnico e funcional do bounded context `SocialPublishing` na plataforma AdMetricsPro. O módulo cobre conexão orgânica com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e a sugestão de impulsionamento quando um post orgânico ganha tração.

Este documento é a ordem de construção. Nenhuma linha de produção de uma subfase começa antes do teste vermelho correspondente. O ciclo é o do `AGENTS.md`: teste que falha, implementação mínima, refatoração com XML docs e atualização de `/docs`.

## Premissas travadas antes da Fase 1

Estas decisões evitam acoplar o módulo novo ao que já existe e serão registradas nos ADRs indicados em cada fase.

1. **Stack e host.** .NET 10 em `SocialPublishing.Domain`, `SocialPublishing.Contracts`, `SocialPublishing.Application`, `SocialPublishing.Infrastructure`, nos controllers de `src/Backend/WebApi` e nas páginas Blazor Server de `src/Frontend/WebApp`. O frontend não referencia Infrastructure, não instancia `DbContext` e não despacha MediatR. Todo acesso passa por `HttpClient` tipado contra `/api/v1/...` com envelope `Result<T>` (ADR 0020).
2. **Banco do tenant, contexto próprio.** Os dados do módulo vivem no SQL Server dedicado do tenant, com connection string resolvida por `ITenantConnectionResolver` e instância criada por `ITenantDbContextFactory<SocialPublishingDbContext>`. O módulo **não** usa `TenantOperationalDbContext`, **não** injeta `ITenantDbContextAccessor` e **não** lê `OAuthTokenVault`, `Workspace` nem `TenantBranding` por LINQ. A história de migração é exclusiva (`social.__EFMigrationsHistory`). ADR 0043.
3. **Cofre orgânico separado do cofre de mídia paga.** `OrganicConnection` guarda access token e refresh token cifrados com AES-256-CBC via `IEncryptionService` (`AesEncryptionService` do kernel). O envelope inclui propósito `organic:{platform}:{workspaceId}:{connectionId}`. Linhas de Meta Ads, Google Ads, Bing Ads e TikTok Ads (ADR 0026) não são reutilizadas: os escopos orgânicos são outros. `SocialPublishing` não referencia `Integrations.Domain`, `Integrations.Infrastructure`, `Analytics.*` nem `Tenants.Infrastructure`.
4. **Conversa entre módulos só por MediatR.** Consultas de workspace, branding e catálogo de tenants continuam nos módulos donos. O worker e os controllers em `WebApi` (raiz de composição) disparam essas consultas e só então enviam comandos de `SocialPublishing`. O handoff para anúncio e para o Creative Hub usa tipos de `SocialPublishing.Contracts` (ADR 0009).
5. **Fluxo de negócio.** Validação e regra de negócio retornam `Result` / `Result<T>` com `Error.Validation`, `Error.NotFound`, `Error.Conflict` ou `Error.Unauthorized`. `ValidationBehavior` já converte FluentValidation em `Result.Failure`. Adaptadores HTTP capturam falha de rede na borda e devolvem `Result.Failure`; não propagam exceção de regra.
6. **Worker no processo.** Publicação assíncrona usa `BackgroundService` no mesmo desenho de `MetaSynchronizationWorker` e `DunningBackgroundService`. A durabilidade é uma tabela outbox no banco do tenant, com concorrência otimista. Não entra fila externa nesta entrega.
7. **Portal público no domínio da agência.** O link de aprovação segue o precedente de `PublicReportsController` e `SharedReportPage.razor`: rota anônima, layout vazio, branding carregado por `GetTenantBrandingQuery`. O tenant é resolvido pelo host CNAME (ADR 0025) ou pelo slug em `/t/{slug}/approve/{token}` **antes** de abrir o banco. O token opaco é persistido só como SHA-256.
8. **Kill switch.** A publicação respeita uma flag nova `killswitch.social-publishing.dispatch`, no mecanismo já aceito pelo ADR 0015. A avaliação ocorre no worker, via contrato do Master, antes de chamar rede externa.

### Catálogo de erros de negócio

| Código | Quando |
| :--- | :--- |
| `Social.WorkspaceRequired` | `WorkspaceId` vazio. |
| `Social.ConnectionNotFound` | Conexão orgânica inexistente no workspace. |
| `Social.TokenRevoked` | Conexão revogada ou refresh rejeitado pela rede. |
| `Social.OAuthStateInvalid` | State ausente, expirado, adulterado ou de outro tenant/workspace. |
| `Social.PlatformUnsupported` | Rede fora de `Instagram`, `FacebookPage`, `TikTok`. |
| `Social.MediaAspectInvalid` | Mídia fora da proporção exigida pelo formato (Reels e TikTok: 9:16). |
| `Social.CaptionTooLong` | Legenda acima do teto da rede. |
| `Social.HashtagPolicyViolated` | Hashtag duplicada, com espaço ou acima do teto configurado. |
| `Social.PillarWeightsInvalid` | Pesos dos pilares somam valor diferente de 100 ou algum peso é negativo. |
| `Social.BrandVoiceNotFound` | Workspace sem DNA de marca persistido. |
| `Social.SlotNotReschedulable` | Post em `Publishing`, `Published` ou `Cancelled`. |
| `Social.InvalidTransition` | Transição de status não permitida pela máquina de estados. |
| `Social.ApprovalTokenInvalid` | Token ausente, expirado, revogado ou de outro workspace. O cliente recebe a mesma resposta nos três casos. |
| `Social.PublishRateLimited` | Rede respondeu 429 ou quota esgotada; o job volta para retentativa. |
| `Social.PublishFailed` | Falha definitiva após o teto de tentativas. |
| `Social.BoostAlreadyRequested` | Já existe sugestão aberta para o mesmo post. |

### Máquina de estados do post

`Draft` → `PendingApproval` → `Approved` → `Scheduled` → `Publishing` → `Published`.

Saídas laterais: `Rejected` e `ChangesRequested` a partir de `PendingApproval`; `Failed` a partir de `Publishing`; `Cancelled` a partir de `Draft`, `PendingApproval`, `Approved`, `Scheduled` ou `Failed`. Reagendamento só em `Draft`, `PendingApproval`, `Approved` e `Scheduled`.

Formatos: `FeedImage`, `FeedCarousel`, `Reel`, `TikTokVideo`. Pilares: `Authority`, `Sale`, `Connection`, `SocialProof`.

---

## 1. Tabela Resumo das Fases

| Fase | Objetivo Central | Camadas Impactadas | Entregável Principal |
| :--- | :--- | :--- | :--- |
| **1. Fundação do domínio** | Abrir o bounded context, os agregados e o `SocialPublishingDbContext` no banco do tenant, sem vazar persistência para outros módulos. | `Domain`, `Application`, `Infrastructure` | Projetos na solution, migração `social`, repositórios, UnitOfWork e ADR 0043. |
| **2. Conectores OAuth2 e storage** | Conectar Instagram, Facebook Pages e TikTok Content Posting e guardar tokens cifrados com AES-256. | `Domain`, `Application`, `Infrastructure` | Fluxo authorize/callback/refresh/revoke, cofre orgânico e porta de mídia. ADR 0044. |
| **3. Motor de IA** | Persistir o Brand Voice e gerar cronograma por pilares mais legendas e hashtags por rede. | `Domain`, `Application`, `Infrastructure` | Planner determinístico, porta `ISocialCreativeGenerator` e rascunhos de calendário. ADR 0045. |
| **4. Agendador e worker** | Publicar no horário, com retentativa, idempotência e respeito a rate limit. | `Domain`, `Application`, `Infrastructure` | Outbox `PublicationJob`, adaptadores de publicação e `SocialPublishingDispatchWorker`. ADR 0046. |
| **5. Blazor e aprovação white-label** | Operar o calendário no servidor Blazor e aprovar sem login no domínio da agência. | `Blazor Server` (consumo exclusivo da Web API; contratos em `Application`) | `SocialCalendarGrid.razor`, drag-and-drop de reagendamento e portal `/approve/{token}`. ADR 0047. |
| **6. Sinergia com mídia paga** | Detectar tração orgânica, registrar o ativo no Creative Hub e sugerir impulsionamento. | `Domain`, `Application`, `Infrastructure`, `Blazor Server` | Notificação in-memory, sugestão de boost e handoff para Integrations. ADR 0048. |

A documentação viva (`docs/modules/social-publishing.md`, XML `<summary>` / `<param>` / `<returns>`, OpenAPI com `[EndpointSummary]` e `[ProducesResponseType]`) fecha cada fase. Scalar permanece em `/scalar/v1`.

---

## 2. Detalhamento Passo a Passo das Fases

### Fase 1 — Fundação do domínio e do bounded context

Objetivo: existir um módulo compilável, testado e incapaz de enxergar o `DbContext` alheio. Nenhuma chamada a Meta ou TikTok nesta fase.

#### Subfase 1.1 — Projetos, governança e especificação inicial

* **1.1.1 (TDD - Red):** Em `tests/UnitTests/Backend/Compliance/AgentsArchitectureComplianceTests.cs`, acrescentar casos que falham enquanto os assemblies não existem e, assim que existirem, passam a exigir:
  * `SocialPublishing.Domain` sem referência a `EntityFrameworkCore`, `AspNetCore` ou qualquer `*.Infrastructure`.
  * `SocialPublishing.Application` sem referência a `SocialPublishing.Infrastructure`, `WebApi`, `Integrations.*`, `Analytics.*` ou `Tenants.Infrastructure`.
  * `SocialPublishing.Infrastructure` sem referência a `Integrations.Infrastructure` e `Analytics.Infrastructure`.
  * `WebApp` sem referência a `SocialPublishing.Infrastructure`.
  * Todo handler novo incluído na varredura `AllMediatRRequestHandlers_MustReturnResultOrResultOfT`.
  * Todo método assíncrono de repositório do módulo com `CancellationToken`.
  * `XmlDocumentationComplianceTests` listando os quatro assemblies novos.
* **1.1.2 (TDD - Green):** Criar os projetos `net10.0` com `Nullable`, `ImplicitUsings` e `GenerateDocumentationFile`, referenciando apenas `BuildingBlocks.Domain` (Domain), `BuildingBlocks.Application` + Domain + Contracts (Application) e Application + `BuildingBlocks.Infrastructure` (Infrastructure). Registrar os projetos em `AdMetricsPro.sln` e as referências de teste em `UnitTests.Backend`, `IntegrationTests` e `AcceptanceTests`. `WebApi` referencia Application e Infrastructure. `SocialPublishing.Contracts` referencia somente `BuildingBlocks.Domain` e `BuildingBlocks.Application` (tipos de notificação e comando de integração, sem EF).
* **1.1.3 (TDD - Green):** Extensão `AddSocialPublishingModule(IServiceCollection, IConfiguration)` registrando `AddMessaging` do assembly de Application. Chamada única em `Program.cs`.
* **1.1.4 (Refactor / XML Docs):** Abrir `docs/modules/social-publishing.md` com visão, máquina de estados, catálogo de erros e a seção “fora de escopo desta fase”. Criar `docs/adr/0043-social-publishing-bounded-context-and-tenant-database.md` no formato Nygard (Status, Contexto, Decisão, Consequências) e uma linha no catálogo `docs/adr/README.md`. XML `<summary>` em todo tipo público criado no passo verde.

#### Subfase 1.2 — Modelo de domínio puro

Agregados e value objects, sem EF. Identidade `Guid`. Eventos de domínio pelo contrato já existente de `AggregateRoot<TId>`.

* **1.2.1 (TDD - Red):** `OrganicConnectionTests` cobre `OrganicConnection.Create`:
  * sucesso com `WorkspaceId`, plataforma, `ExternalAccountId`, nome de exibição e escopos;
  * `Result.Failure(Social.WorkspaceRequired)` quando o workspace é `Guid.Empty`;
  * `Result.Failure(Social.PlatformUnsupported)` para plataforma desconhecida;
  * `MarkTokensRotated` recusa connection `Revoked` com `Social.TokenRevoked`;
  * `Revoke` é idempotente e emite evento `OrganicConnectionRevoked`.
  Tokens em claro não aparecem em propriedades públicas. O agregado guarda somente `EncryptedAccessToken`, `EncryptedRefreshToken` e expiração UTC.
* **1.2.2 (TDD - Red):** `OrganicPostTests` cobre criação em `Draft`, teto de legenda (Instagram e Facebook Page 2.200 caracteres; TikTok 2.200 caracteres — constantes nomeadas `CaptionLimits`, revisáveis quando a especificação da rede mudar), carrossel com 2 a 10 itens, Reels/TikTok sem mídia com `Social.MediaAspectInvalid` se a proporção informada não for 9:16 (tolerância de 1%), e cada transição ilegal da máquina de estados com `Social.InvalidTransition`. Reagendamento de `Published` retorna `Social.SlotNotReschedulable`. Horário persistido em UTC; o fuso do workspace entra só como dado de entrada para converter.
* **1.2.3 (TDD - Red):** `BrandVoiceProfileTests` cobre tom, persona, links obrigatórios, termos proibidos, política de emoji e pesos dos quatro pilares. Soma diferente de 100 ou peso negativo devolve `Social.PillarWeightsInvalid`. `HashtagGroupTests` rejeita duplicata, espaço interno e quantidade acima do teto do perfil (teto duro de 30 no Instagram, 8 no TikTok, nunca acima do teto do perfil).
* **1.2.4 (TDD - Green):** Implementar os tipos em `SocialPublishing.Domain`: `OrganicPlatform`, `OrganicPostFormat`, `OrganicPostStatus`, `ContentPillar`, `OrganicConnection`, `OrganicPost`, `OrganicMediaAsset`, `BrandVoiceProfile`, `HashtagGroup`, `CaptionLimits`, `AspectRatio`. Fábricas estáticas retornam `Result<T>`. Coleções internas encapsuladas. Nenhum `throw` para regra de negócio.
* **1.2.5 (Refactor / XML Docs):** `<summary>`, `<param>` e `<returns>` em tipos, fábricas e propriedades públicas. Seção “Modelo de domínio” em `docs/modules/social-publishing.md` com os invariantes e um exemplo JSON do post em `Draft`.

#### Subfase 1.3 — Persistência no banco do tenant

* **1.3.1 (TDD - Red):** Teste de integração `SocialPublishingDbContextTests` (SQL Server real, mesmo host dos testes de `IntegrationTests`, banco efêmero por tenant de teste) afirma que:
  * `ITenantDbContextFactory<SocialPublishingDbContext>` abre o banco cuja connection string o resolver devolveu para aquele `TenantId`;
  * dois tenants não enxergam a conexão um do outro;
  * a tabela de histórico é `social.__EFMigrationsHistory` e nenhuma migração escreve em `__EFMigrationsHistory` do operacional;
  * round-trip de `OrganicConnection` persiste ciphertext e não a string original do token.
* **1.3.2 (TDD - Red):** Testes de `IOrganicConnectionRepository`, `IOrganicPostRepository` e `IBrandVoiceRepository` (interfaces na Application) cobrindo `GetByIdAsync`, `ListByWorkspaceAsync`, `AddAsync` e `Update` com `CancellationToken` cancelado antes da chamada.
* **1.3.3 (TDD - Green):** `SocialPublishingDbContext` com `DbSet` apenas dos agregados deste módulo. Configurações `IEntityTypeConfiguration` no schema `social`. `SocialPublishingUnitOfWork.CommitAsync` dispara eventos de domínio e persiste. Repositórios recebem o contexto por escopo; não há `DbContext` compartilhado com outros módulos. Migração inicial aplicada pelo provisionamento já existente: ao final de `ITenantProvisioningService`, invocar também `MigrateAsync` deste contexto (extensão no módulo, chamada a partir do host, sem o módulo Master referenciar repositórios de Social). Para tenants já provisionados, um hosted service de startup aplica a migração pendente em cada banco ativo, no mesmo estilo de `MasterDatabaseMigrationHostedService`, iterando tenants via `GetTenantsQuery` na raiz de composição.
* **1.3.4 (Refactor / XML Docs):** Documentar no ADR 0043 a história de migração separada e a proibição de `Ignore` cruzado com tabelas operacionais. Atualizar a spec com o diagrama lógico das tabelas `social.OrganicConnections`, `social.OrganicPosts`, `social.OrganicMediaAssets`, `social.BrandVoiceProfiles`.

#### Subfase 1.4 — Primeiro contrato HTTP do módulo

* **1.4.1 (TDD - Red):** Teste de aceitação com `WebApplicationFactory` para `GET /api/v1/social/status?workspaceId={guid}`:
  * sem tenant resolvido → falha de autorização já padronizada pelo pipeline atual;
  * com header `X-Tenant-Id` válido e workspace inexistente → `404` e `Result.Failure` (`Social.WorkspaceRequired` ou o código de workspace do módulo Tenants, composto no controller);
  * com workspace válido → `200` e `Result<SocialModuleStatusDto>` com `connections: 0` e `scheduledPosts: 0`.
  O teste exige `[EndpointSummary]` e `[ProducesResponseType]` de 200 e 404 no método do controller (reflexão sobre os atributos).
* **1.4.2 (TDD - Green):** `GetSocialModuleStatusQuery` / handler retornando contagens via repositórios. `SocialController` em `WebApi/Controllers/v1` apenas traduz `Result` em status HTTP. Autorização com a permissão nova `social.read`, adicionada no catálogo RBAC **do módulo Tenants** (tarefa no próprio módulo Tenants: teste do catálogo primeiro, depois a chave). SocialPublishing não grava a tabela de permissões.
* **1.4.3 (Refactor / XML Docs):** Spec da rota em `docs/modules/social-publishing.md` com request, response e erros. XML docs nos DTOs. Scalar deve listar a operação em desenvolvimento.

---

### Fase 2 — Conectores OAuth2 orgânicos e storage cifrado

Objetivo: uma agência conecta Instagram profissional, a Page do Facebook vinculada e uma conta TikTok de publicação, com segredo ilegível no banco. O cofre de anúncios permanece intacto.

Escopos previstos (confirmados na documentação vigente da rede no momento da implementação, gravados em `OAuthNetworkOptions` e não espalhados pelo domínio):

* Instagram + Facebook Pages: permissões de listagem de Pages, leitura de conta profissional e publicação de conteúdo (feed, carrossel e Reels).
* TikTok: escopos de upload e publicação da Content Posting API, distintos dos escopos da Marketing API já usados em Integrations.

Segredos de aplicativo (`AppId`, `AppSecret`) entram por `.env`, ADR 0018. Testes usam `TestHttpMessageHandler`. Nenhuma suíte chama a internet.

#### Subfase 2.1 — State anti-CSRF e início do fluxo

* **2.1.1 (TDD - Red):** `OrganicOAuthStateServiceTests` exige state HMAC-SHA256 com `TenantId`, `WorkspaceId`, `OrganicPlatform`, `RedirectUri` e `IssuedAtUtc`. TTL de 15 minutos. Payload adulterado, expirado ou de outro workspace produz `Social.OAuthStateInvalid`. O state não contém token de acesso.
* **2.1.2 (TDD - Red):** `StartOrganicAuthorizationQueryTests` para as três plataformas devolve URL de autorização HTTPS do provedor correto e um state verificável. Plataforma inválida → `Social.PlatformUnsupported`. Workspace vazio → `Social.WorkspaceRequired`.
* **2.1.3 (TDD - Green):** `IOrganicOAuthStateService` e `IOrganicPlatformAdapter.BuildAuthorizationUrl` implementados dentro do módulo. Três adaptadores (`MetaInstagramOrganicAdapter`, `MetaFacebookPageOrganicAdapter`, `TikTokContentPostingAdapter`) com `HttpClient` nomeado. O adapter do Facebook Page e o do Instagram podem compartilhar o host OAuth da Meta, mas permanecem classes distintas porque os recursos publicados depois divergem.
* **2.1.4 (Refactor / XML Docs):** ADR `docs/adr/0044-organic-oauth-vault-and-aes-256.md` explicando por que o vault de anúncios não é estendido. Spec com o JSON de `StartOrganicAuthorizationResponse` (`authorizationUrl`, `state`, `expiresAtUtc`).

#### Subfase 2.2 — Callback, troca de código e cofre

* **2.2.1 (TDD - Red):** `CompleteOrganicAuthorizationCommandTests` com handler HTTP falso:
  * state válido + code aceito grava `OrganicConnection` cujo access token persistido é diferente do token em claro e cujo decrypt com o propósito certo recupera o valor;
  * decrypt com propósito de outro workspace falha com erro de cofre e não devolve o segredo;
  * state inválido não chama o endpoint de troca;
  * erro do provedor vira `Result.Failure` com código estável (`Social.OAuthExchangeFailed`), sem exceção;
  * segunda conclusão do mesmo `ExternalAccountId` no workspace atualiza a conexão (`Result` de sucesso com o mesmo `ConnectionId`), não duplica linha.
* **2.2.2 (TDD - Green):** Handler valida state, chama `ExchangeCodeAsync`, protege os dois tokens com `IEncryptionService` e um envelope de propósito, persiste via repositório e `CommitAsync`. A resposta da API traz `connectionId`, plataforma, nome da conta e expiração. Nunca devolve token.
* **2.2.3 (TDD - Green):** Rotas `POST /api/v1/social/connections/authorize` e `POST /api/v1/social/connections/callback`, permissão `social.connections.manage`, atributos OpenAPI 200, 400, 422.
* **2.2.4 (Refactor / XML Docs):** Exemplos de payload e de erro na spec. XML docs no protetor de token, nos adaptadores e nos DTOs. Teste de conformidade confirma que nenhum logger do módulo recebe a string do token (passar um `ILogger` falso e falhar se a mensagem contiver o segredo de teste).

#### Subfase 2.3 — Refresh preventivo e revogação

* **2.3.1 (TDD - Red):** `RefreshOrganicConnectionCommandTests`:
  * conexão com access token a menos de 7 dias do vencimento (Meta long-lived) ou já expirado (TikTok) renova e substitui o ciphertext;
  * refresh rejeitado marca a conexão como `Revoked` e retorna `Social.TokenRevoked`;
  * conexão já revogada não chama a rede.
* **2.3.2 (TDD - Red):** `RevokeOrganicConnectionCommandTests` remove a capacidade local mesmo se a rede falhar ao revogar, e registra o resultado remoto no agregado (`RemoteRevocationSucceeded`). O ciphertext deixa de ser utilizável.
* **2.3.3 (TDD - Green):** Comandos, handlers, rotas `POST /api/v1/social/connections/{id}/refresh` e `DELETE /api/v1/social/connections/{id}`. Listagem `GET /api/v1/social/connections?workspaceId=` sem segredos.
* **2.3.4 (Refactor / XML Docs):** Fechar a seção OAuth da spec com os três erros de borda: state expirado, code rejeitado, refresh rejeitado.

#### Subfase 2.4 — Metadados de mídia e armazenamento de binário

Vídeo e imagem não entram em coluna SQL. O banco guarda chave do objeto, mime type, duração, largura, altura e proporção.

* **2.4.1 (TDD - Red):** `OrganicMediaAssetTests` e `RegisterOrganicMediaCommandTests`:
  * imagem de feed 4:5 e 1:1 aceitas para `FeedImage`;
  * carrossel rejeitado com item de proporção diferente do primeiro;
  * Reel e TikTok rejeitados quando a proporção não é 9:16;
  * mime type fora de `image/jpeg`, `image/png`, `video/mp4` rejeitado;
  * o binário não aparece no comando persistido, só a chave devolvida pela porta `IOrganicMediaStore`.
* **2.4.2 (TDD - Green):** Porta `IOrganicMediaStore` (`SaveAsync`, `OpenReadAsync`, `DeleteAsync`) com adaptador local em disco para desenvolvimento e adaptador S3-compatível atrás de opções (bucket, prefixo por `tenantId/workspaceId`). O handler valida o domínio **antes** de pedir o armazenamento definitivo; upload inválido não deixa objeto órfão (se a validação de proporção exigir o arquivo, o adaptador grava em área temporária e o handler apaga no `Result` de falha).
* **2.4.3 (TDD - Green):** `POST /api/v1/social/media` multipart, limite de tamanho configurado, resposta `Result<OrganicMediaAssetDto>` 201. 422 para proporção inválida.
* **2.4.4 (Refactor / XML Docs):** Registrar na spec o contrato da porta e a decisão de não persistir binário no SQL. Constantes de proporção documentadas junto dos testes que as fixam.

---

### Fase 3 — AI Social Copilot

Objetivo: separar o que é determinístico (distribuição dos pilares) do que depende de modelo (texto). O calendário é reproduzível em teste sem rede neural. O provedor de IA é um adaptador substituível.

#### Subfase 3.1 — Ingestão do Brand Voice

* **3.1.1 (TDD - Red):** `UpsertBrandVoiceCommandTests` persiste tom, persona, links, termos proibidos, idioma, política de emoji, teto de hashtags e pesos. Segunda gravação no mesmo workspace atualiza a mesma linha. Pesos inválidos nem chamam o repositório.
* **3.1.2 (TDD - Green):** `PUT /api/v1/social/brand-voice/{workspaceId}` e `GET` correspondente. Permissão `social.copilot.generate` para gravar; `social.read` para ler.
* **3.1.3 (Refactor / XML Docs):** JSON de entrada e de `Result<BrandVoiceDto>` na spec, incluindo o caso de borda dos pesos.

#### Subfase 3.2 — Cronograma editorial determinístico

* **3.2.1 (TDD - Red):** `EditorialCalendarPlannerTests` sem qualquer dobro de IA:
  * plano semanal com 5 posts e pesos 40/20/25/15 produz contagem de pilares compatível com o maior resto (método das quotas), total exato de 5;
  * dois pilares `Sale` não caem em dias consecutivos quando a contagem permite espaçamento;
  * plano mensal respeita o fuso do workspace (um slot de “segunda 10:00” em `America/Sao_Paulo` vira o UTC certo, inclusive na fronteira de horário de verão se o fuso tiver regra);
  * semente explícita torna a ordem estável entre duas execuções;
  * Brand Voice ausente → `Social.BrandVoiceNotFound`;
  * data final anterior à inicial → `Error.Validation`.
* **3.2.2 (TDD - Green):** Serviço de domínio `EditorialCalendarPlanner` puro. Entrada: intervalo, fuso, posts por semana, pesos, semente. Saída: lista de `EditorialSlot` (`localDate`, `utcInstant`, `pillar`, `network`, `format`) ainda sem texto.
* **3.2.3 (Refactor / XML Docs):** Documentar o algoritmo de quotas e a regra de espaçamento de `Sale` na spec, com um exemplo de semana.

#### Subfase 3.3 — Legendas, copys e hashtags por rede

* **3.3.1 (TDD - Red):** `GenerateSlotCopyCommandTests` com `ISocialCreativeGenerator` falso:
  * o prompt montado contém persona, tom, termos proibidos, pilar e rede, e não contém token OAuth;
  * resposta do gerador com termo proibido é recusada com `Error.Validation` e não vira post;
  * legenda acima de `CaptionLimits` da rede é recusada com `Social.CaptionTooLong`;
  * hashtags voltam em três grupos (`Brand`, `Niche`, `Reach`), sem duplicata, respeitando o teto;
  * falha do provedor (timeout, HTTP 5xx) vira `Result.Failure(Social.CopyGenerationFailed)` e o slot permanece sem texto;
  * Instagram, Facebook Page e TikTok recebem instruções de formato diferentes (carrossel pede sequência de cards; Reel e TikTok pedem gancho falado curto). O teste inspeciona o `CopyBrief`, não uma URL real.
* **3.3.2 (TDD - Green):** Montador de brief na Application. Adaptador HTTP em Infrastructure (API compatível com chat completions), opções `SocialAi:BaseUrl`, `SocialAi:Model`, `SocialAi:ApiKey` via configuração / `.env`. Timeout configurado. O adaptador não lança para o handler: traduz exceção de transporte em `Result.Failure`. Temperatura e teto de tokens ficam nas opções, cobertos por teste do adaptador com `TestHttpMessageHandler`.
* **3.3.3 (Refactor / XML Docs):** ADR `docs/adr/0045-ai-social-copilot-brand-voice-and-editorial-plan.md`. Spec com o JSON do brief e do `GeneratedCopyDto` (`caption`, `hashtagGroups`, `network`, `pillar`, `warnings`).

#### Subfase 3.4 — Materializar o plano em rascunhos

* **3.4.1 (TDD - Red):** `GenerateEditorialPlanCommandTests` cria N `OrganicPost` em `Draft`, cada um com pilar, rede, formato, `ScheduledAtUtc` e cópia aceita. Se a cópia de um slot falha, os slots anteriores permanecem (resultado parcial explícito: `GeneratedPlanDto` com `createdPostIds` e `failedSlots`), e a transação por slot usa `CommitAsync` próprio. Reexecução com a mesma chave de idempotência (`workspaceId + range + seed`) devolve o plano já criado sem duplicar.
* **3.4.2 (TDD - Green):** Comando e rota `POST /api/v1/social/plans:generate`. Consulta `GET /api/v1/social/posts?workspaceId&from&to` para o calendário ler a grade.
* **3.4.3 (Refactor / XML Docs):** Casos de borda na spec: Brand Voice ausente, provedor indisponível no meio do plano, chave de idempotência repetida.

---

### Fase 4 — Agendador, outbox e background worker

Objetivo: no horário UTC do slot aprovado, publicar exatamente uma vez, recuar diante de 429 e desligar o disparo sem deploy quando o kill switch armar.

#### Subfase 4.1 — Aprovação interna e reagendamento

A aprovação da agência (usuário autenticado) muda o status. O portal do cliente final entra na Fase 5; o domínio já aceita as transições.

* **4.1.1 (TDD - Red):** `SubmitPostForApprovalCommandTests`, `ApprovePostCommandTests` e `ReschedulePostCommandTests` cobrem cada transição válida e as inválidas (`Social.InvalidTransition`, `Social.SlotNotReschedulable`). Reagendar grava o UTC novo e um histórico `ScheduleRevision` (quem, quando, de/para). Horário no passado em status `Scheduled` retorna validação.
* **4.1.2 (TDD - Green):** Comandos, handlers e rotas `POST /api/v1/social/posts/{id}:submit`, `:approve`, `:reject`, `:reschedule`. Body de reagendamento: `{ "scheduledAtUtc": "..." }`.
* **4.1.3 (Refactor / XML Docs):** Tabela de transições na spec, espelhando os testes.

#### Subfase 4.2 — Outbox e reivindicação otimista

* **4.2.1 (TDD - Red):** `PublicationJobTests` e teste de integração do repositório:
  * aprovar e agendar um post cria um `PublicationJob` `Pending` com chave `{postId}:{scheduledAtUtc:O}`;
  * reagendar cancela o job anterior e abre outro com chave nova;
  * dois `TryClaimAsync` concorrentes no mesmo job: um recebe o job, o outro recebe `null` (token de concorrência / `RowVersion`);
  * job `Succeeded` não é reivindicado de novo;
  * a chave de idempotência impede segundo insert para o mesmo slot.
* **4.2.2 (TDD - Green):** Entidade `PublicationJob` (`Pending`, `InProgress`, `RetryScheduled`, `Succeeded`, `DeadLetter`), repositório com claim condicional, vínculo 1:1 lógico com o slot via chave única. `CommitAsync` no mesmo contexto do post.
* **4.2.3 (Refactor / XML Docs):** Seção de outbox na spec. ADR 0046 começa aqui e fecha na subfase 4.4.

#### Subfase 4.3 — Adaptadores de publicação

Os adaptadores recebem token já decifrado pelo handler, no escopo da chamada, e não logam o valor. O domínio decide o formato; o adaptador só traduz.

* **4.3.1 (TDD - Red):** Testes com `TestHttpMessageHandler` para:
  * feed de imagem na Page (uma chamada de publicação e persistência do id externo);
  * carrossel Instagram (criação dos itens filhos, container pai, publicação — o teste conta a ordem das requisições);
  * Reel Instagram em vídeo 9:16 (container de vídeo e publicação);
  * TikTok Content Posting (início de upload e publicação do vídeo vertical);
  * HTTP 429 mapeado para `Social.PublishRateLimited` com `retryAfter` quando o header existir;
  * erro funcional da rede mapeado para `Social.PublishFailed` sem throw;
  * resposta de sucesso repetida com o mesmo id externo tratada como sucesso idempotente.
* **4.3.2 (TDD - Green):** Métodos `PublishAsync` nos três adaptadores da Fase 2. Política `OrganicPublishRateLimitPolicy` com o mesmo formato já aceito no ADR 0027, reimplementada neste módulo para não referenciar Integrations:

  \( T = 2^{attempt} \times baseDelay + jitter \)

  Teto padrão de 3 tentativas e atraso máximo de 10 segundos, ambos em opções. A política é função pura testada à parte.
* **4.3.3 (Refactor / XML Docs):** Tabela formato → operação de rede na spec, sem colar segredo nem URL com token. XML docs nos adaptadores.

#### Subfase 4.4 — Worker de disparo

* **4.4.1 (TDD - Red):** `SocialPublishingDispatchWorkerTests` com escopos falsos, no estilo do worker Meta:
  * só tenants `Active` ou `Trial`;
  * por tenant, o accessor recebe o `TenantContext` daquele id antes do comando;
  * kill switch ligado → nenhum `DispatchDuePublicationsCommand`;
  * job devido → claim, chamada do adaptador, `Published` + `Succeeded` quando o `Result` é sucesso;
  * `Social.PublishRateLimited` → job `RetryScheduled` com `NextAttemptAtUtc` da política, post volta a `Scheduled`;
  * após o teto, job `DeadLetter`, post `Failed`, sem nova chamada;
  * cancelamento do host interrompe o laço sem marcar o job como falha definitiva;
  * exceção inesperada de infraestrutura num tenant não impede o tenant seguinte (o worker registra e segue). A falha de negócio continua sendo `Result`, não exceção.
* **4.4.2 (TDD - Green):** `DispatchDuePublicationsCommand` na Application. `SocialPublishingDispatchWorker : BackgroundService` em `WebApi/Background`, opções `Enabled` e `Interval`, registro ao lado dos workers existentes. O worker usa `ISender` para `GetTenantsQuery` e para o comando do módulo. Avaliação da flag `killswitch.social-publishing.dispatch` pelo serviço de feature flag já existente, antes do laço de jobs. A flag em si é criada por uma tarefa do módulo Master (teste do catálogo de flags primeiro).
* **4.4.3 (Refactor / XML Docs):** Concluir ADR `docs/adr/0046-organic-publication-outbox-and-dispatch-worker.md` com a suposição de instância única do host e a proteção extra do `RowVersion`. Spec do ciclo de vida do job e dos códigos 429.

---

### Fase 5 — Interface Blazor Server e aprovação white-label

Objetivo: o gestor opera conexão, copilot e calendário no `WebApp`. O cliente final aprova por link, no domínio da agência, sem usuário da plataforma. Nenhuma página injeta repositório, contexto ou MediatR.

Componentes de página ficam em `src/Frontend/WebApp/Components/Social/`. Peças atômicas reutilizáveis (selo de status, chip de pilar) ficam em `Components/Shared/Components/`, como pede o `AGENTS.md`. Testes bUnit em `tests/UnitTests/Frontend/Social/`.

#### Subfase 5.1 — Cliente HTTP e conexões

* **5.1.1 (TDD - Red):** bUnit de `SocialConnectionsPanel.razor`:
  * lista vazia mostra estado vazio e os três botões de conectar;
  * clique em conectar chama `ISocialPublishingClient.StartAuthorizationAsync` e navega para a URL devolvida;
  * `Result` de falha exibe a descrição do erro e não navega;
  * cartão de conexão conectada oferece revogar e não renderiza token.
  Teste do cliente HTTP com `HttpMessageHandler` falso: header `X-Tenant-Id`, rota versionada, desserialização do envelope `Result<T>`.
* **5.1.2 (TDD - Green):** `ISocialPublishingClient` / `SocialPublishingClient` registrados no `WebApp`. Página `/social/connections` dentro do layout autenticado do tenant, com o tema white-label já aplicado pelo `TenantMainLayout`.
* **5.1.3 (Refactor / XML Docs):** Seção de frontend na spec: rotas Blazor, serviços e a proibição de acesso a banco. XML docs na interface do cliente.

#### Subfase 5.2 — Grade mensal e semanal com drag-and-drop

* **5.2.1 (TDD - Red):** bUnit de `SocialCalendarGrid.razor`:
  * modo `Month` renderiza as células do mês civil no fuso do workspace, incluindo dias de preenchimento;
  * modo `Week` renderiza sete colunas;
  * cada post aparece no dia do seu `ScheduledAtUtc` convertido ao fuso;
  * post `Published` não expõe alça de arraste;
  * soltar um post `Scheduled` em outro dia chama `RescheduleAsync` com o UTC calculado (mantém a hora local);
  * se o `Result` falha, o post volta para a célula de origem e a mensagem de `Social.SlotNotReschedulable` aparece;
  * troca de workspace recarrega a grade (estado não vaza entre clientes da agência).
* **5.2.2 (TDD - Green):** Componente com parâmetros `View`, `AnchorDate`, `Posts`, `TimeZone`. Evento de drop invoca o cliente HTTP. Interop mínima em `wwwroot/js/socialCalendar.js` apenas se o evento HTML5 não bastar no circuito Blazor Server; a regra de negócio permanece no comando da Fase 4, nunca no JavaScript. Página `/social/calendar`.
* **5.2.3 (Refactor / XML Docs):** Documentar modos da grade, fuso e o contrato do drop. XML docs nos parâmetros públicos do componente.

#### Subfase 5.3 — Portal de aprovação compartilhável

O tenant já está resolvido quando o handler abre `SocialPublishingDbContext`. Link canônico: `https://{dominio-da-agencia}/approve/{token}`. Sem CNAME: `https://{slug}.app.../approve/{token}` ou `/t/{slug}/approve/{token}`.

* **5.3.1 (TDD - Red):** `CreateApprovalLinkCommandTests`:
  * gera token opaco de 32 bytes, persiste só o SHA-256, devolve o token em claro uma única vez;
  * expiração obrigatória;
  * segundo link revoga o anterior do mesmo pacote.
* **5.3.2 (TDD - Red):** `GetPublicApprovalBoardQueryTests` e `DecidePublicApprovalCommandTests`:
  * hash compatível e prazo válido devolvem posts `PendingApproval` e o branding ainda **não** é lido pelo handler de Social (o teste garante que o handler não depende de `TenantBranding`);
  * token desconhecido, expirado ou revogado produzem o mesmo `Social.ApprovalTokenInvalid`;
  * `Approve` move os posts selecionados para `Approved`; `RequestChanges` grava comentário e status `ChangesRequested`;
  * decisão em post de outro workspace no mesmo tenant falha;
  * ausência de `TenantContext` falha antes do repositório.
* **5.3.3 (TDD - Red):** Aceitação de `GET /api/v1/public/social/approvals/{token}` e `POST .../decisions` com `[AllowAnonymous]`, 200 e 404, sem vazar se o token existiu. O controller de `WebApi` compõe o DTO final: envia a query de Social e, em sucesso, `GetTenantBrandingQuery`, e monta `PublicApprovalBoardDto` (`posts`, `primaryColor`, `logoUrl`, `agencyName`).
* **5.3.4 (TDD - Red):** bUnit de `PublicApprovalPage.razor` em `EmptyLayout`:
  * aplica CSS variables do branding recebido;
  * não mostra chrome de login da AdMetricsPro;
  * botão aprovar envia os ids marcados;
  * 404 mostra o mesmo estado genérico de link indisponível, sem distinguir expirado de inexistente.
* **5.3.5 (TDD - Green):** Comandos, queries, `PublicSocialApprovalsController`, página `/approve/{token}` e `/t/{slug}/approve/{token}`. Emissão do link na grade autenticada (`POST /api/v1/social/approval-links`).
* **5.3.6 (Refactor / XML Docs):** ADR `docs/adr/0047-white-label-organic-approval-portal.md`. Spec com os JSON público e autenticado, a regra do hash e a composição de branding na raiz `WebApi`.

#### Subfase 5.4 — Tela do copilot

* **5.4.1 (TDD - Red):** bUnit de `BrandVoiceForm.razor` e `GeneratePlanDialog.razor`: formulário impede submit quando a soma dos pesos não é 100; diálogo de geração chama o cliente com intervalo, semente e posts por semana; falha parcial (`failedSlots`) lista os slots que ficaram sem texto e mantém o link para os posts criados.
* **5.4.2 (TDD - Green):** Componentes e entrada no calendário “Gerar cronograma”. Consumo só do cliente HTTP.
* **5.4.3 (Refactor / XML Docs):** Fluxo de ponta a ponta na spec: Brand Voice → gerar → revisar na grade → enviar para aprovação → link público.

---

### Fase 6 — Sinergia orgânico + mídia paga e Creative Hub

Objetivo: um post publicado com engajamento fora da linha de base vira uma sugestão. Aceitar a sugestão pede ao módulo de anúncios um rascunho de impulsionamento e registra o ativo no Creative Hub. SocialPublishing não chama a Marketing API.

Contratos em `SocialPublishing.Contracts` (sem repositório e sem EF):

* `OrganicTractionDetectedNotification` (`INotification`): `TenantId`, `WorkspaceId`, `OrganicPostId`, rede, `MediaFingerprint`, permalink, métricas e `DetectedAtUtc`.
* `CreatePaidBoostDraftCommand` (`ICommand<Guid>`): pedido de rascunho de anúncio a partir do post orgânico. O handler concreto vive em `Integrations.Application`. SocialPublishing apenas publica o comando via `ISender` quando o usuário aceita.

`Integrations.Application` e `Analytics.Application` podem referenciar `SocialPublishing.Contracts`. O teste de conformidade da Fase 1 continua proibindo referência a Domain, Application ou Infrastructure de Social.

#### Subfase 6.1 — Detecção de tração

* **6.1.1 (TDD - Red):** `OrganicTractionDetectorTests` com série do próprio workspace:
  * janela mínima de 3 posts publicados com impressões; abaixo disso `OrganicTraction.InsufficientData` e nenhuma notificação;
  * engajamento \((curtidas + comentários + compartilhamentos + salvamentos) / impressões\);
  * linha de base = mediana dos posts do workspace na mesma rede nos últimos 28 dias;
  * anomalia quando o post tem impressões mínimas (constante nomeada, default 1.000) e engajamento ≥ 2,0× a mediana;
  * post já ligado a uma sugestão `Open` não dispara outra (`Social.BoostAlreadyRequested` na criação da sugestão);
  * detector é função pura: mesma série, mesmo resultado.
* **6.1.2 (TDD - Green):** `OrganicTractionDetector` no domínio. Coleta das métricas atrás da porta `IOrganicInsightsReader` (adaptador chama a API de insights orgânicos com o token decifrado). Falha de leitura vira `Result.Failure` e não cria sugestão.
* **6.1.3 (Refactor / XML Docs):** Fórmula, limiares e o caso de dados insuficientes na spec.

#### Subfase 6.2 — Notificação e Creative Hub

* **6.2.1 (TDD - Red):** Teste do handler em Analytics (`LinkOrganicAssetWhenTractionDetected`) com notificação falsa:
  * upsert no Creative Hub pela `MediaFingerprint` sem duplicar o ativo;
  * o teste de referência de projeto falha se o handler usar tipo de `SocialPublishing.Infrastructure`;
  * fingerprint ausente não quebra o handler: `Result.Failure` registrado e o post orgânico permanece publicado.
  O teste vive em `tests/UnitTests/Backend/Analytics/` e usa o repositório/porta que o Creative Hub já possui (`ICreativeHubDataProvider` ou um comando novo do próprio Analytics, escrito primeiro em vermelho).
* **6.2.2 (TDD - Red):** No módulo Social, `ScanOrganicTractionCommandTests` persiste `PaidBoostSuggestion` em `Open` e publica `OrganicTractionDetectedNotification` somente após `CommitAsync` bem-sucedido.
* **6.2.3 (TDD - Green):** Implementar o scan (invocado pelo mesmo worker, em passo separado do disparo de publicação, respeitando o kill switch). Handler de Analytics atualiza o hub. Social não referencia o projeto Analytics.
* **6.2.4 (Refactor / XML Docs):** Descrever o fluxo da notificação na spec e no ADR 0048.

#### Subfase 6.3 — Aceite da sugestão e rascunho de anúncio

* **6.3.1 (TDD - Red):** `AcceptPaidBoostSuggestionCommandTests`:
  * sugestão `Open` envia `CreatePaidBoostDraftCommand` e, se o `Result<Guid>` volta com o id do rascunho, marca `Accepted` e guarda `PaidDraftId`;
  * falha do Integrations mantém a sugestão `Open` e devolve o erro original, sem criar segundo envio na mesma requisição;
  * `Dismiss` não chama Integrations;
  * sugestão de outro workspace → `NotFound`.
* **6.3.2 (TDD - Red):** Handler em `Integrations.Application` para `CreatePaidBoostDraftCommand`:
  * rede orgânica Instagram ou Facebook Page produz rascunho Meta Ads; TikTok orgânico produz rascunho TikTok Ads;
  * não há conta de anúncios conectada no workspace → `Result.Failure` com código do módulo de Integrations já existente para conta ausente;
  * o rascunho nasce pausado, com a mídia e a legenda como ponto de partida, e entra no diário de mutações do ADR 0040 quando houver escrita externa. Nesta fase a escrita externa pode ser só o registro local do rascunho, se o fluxo de criação de anúncio ainda exigir confirmação humana na tela de gestão de anúncios. O teste deixa explícito que nada é publicado em leilão sem uma ação posterior já existente de mídia paga.
* **6.3.3 (TDD - Green):** Rotas `GET /api/v1/social/boost-suggestions?workspaceId`, `POST /api/v1/social/boost-suggestions/{id}:accept` e `:dismiss`. Implementar os dois handlers nos módulos donos.
* **6.3.4 (Refactor / XML Docs):** ADR `docs/adr/0048-organic-traction-boost-and-creative-hub.md`. JSON da sugestão e do aceite na spec. Atualizar `docs/modules/creative-hub-and-fatigue-detector.md` com um parágrafo apontando a origem orgânica do fingerprint, sem mover a regra de fadiga para este módulo.

#### Subfase 6.4 — Superfície Blazor da sugestão

* **6.4.1 (TDD - Red):** bUnit de `BoostSuggestionBanner.razor` na grade: sugestão `Open` mostra rede, múltiplo de engajamento e ações Aceitar / Dispensar; Aceitar chama o cliente e, no sucesso, troca o estado para `Accepted` com o id do rascunho; falha mantém `Open` e mostra o erro. bUnit do card de Creative Hub garante que um ativo com origem orgânica renderiza o permalink sem regressão do estado `Fatigued` / `Healthy` já coberto pelos testes atuais.
* **6.4.2 (TDD - Green):** Componentes e métodos no `ISocialPublishingClient`. O banner entra em `SocialCalendarGrid` só como filho; a grade não passa a conhecer Integrations.
* **6.4.3 (Refactor / XML Docs):** Fechar `docs/modules/social-publishing.md` com o fluxo completo, a lista de rotas OpenAPI e a checklist de aceite do `AGENTS.md` (teste antes da implementação, `Result<T>`, XML docs, rota versionada, banco do tenant, docs e ADRs). Entrada no `docs/adr/README.md` para 0043–0048.

---

## 3. Estrutura de Arquivos e Diretórios

Novos artefatos em relação à solution atual. Testes entram nos projetos que já existem (`UnitTests.Backend`, `UnitTests.Frontend`, `IntegrationTests`, `AcceptanceTests`). Controllers ficam em `WebApi`, que é a raiz de composição usada pelos demais módulos.

```text
docs/
├── adr/
│   ├── 0043-social-publishing-bounded-context-and-tenant-database.md
│   ├── 0044-organic-oauth-vault-and-aes-256.md
│   ├── 0045-ai-social-copilot-brand-voice-and-editorial-plan.md
│   ├── 0046-organic-publication-outbox-and-dispatch-worker.md
│   ├── 0047-white-label-organic-approval-portal.md
│   ├── 0048-organic-traction-boost-and-creative-hub.md
│   └── README.md                                          # catálogo atualizado a cada ADR
└── modules/
    ├── social-publishing.md                               # spec viva: regras, JSON, erros, rotas
    └── creative-hub-and-fatigue-detector.md               # parágrafo de origem orgânica na Fase 6

src/Backend/Modules/SocialPublishing/
├── SocialPublishing.Contracts/
│   ├── SocialPublishing.Contracts.csproj
│   ├── Notifications/
│   │   └── OrganicTractionDetectedNotification.cs
│   └── Boost/
│       └── CreatePaidBoostDraftCommand.cs
├── SocialPublishing.Domain/
│   ├── SocialPublishing.Domain.csproj                     # só BuildingBlocks.Domain
│   ├── Connections/
│   │   ├── OrganicConnection.cs
│   │   ├── OrganicPlatform.cs
│   │   └── OrganicConnectionStatus.cs
│   ├── Posts/
│   │   ├── OrganicPost.cs
│   │   ├── OrganicPostStatus.cs
│   │   ├── OrganicPostFormat.cs
│   │   ├── OrganicMediaAsset.cs
│   │   ├── ScheduleRevision.cs
│   │   ├── CaptionLimits.cs
│   │   └── AspectRatio.cs
│   ├── Brand/
│   │   ├── BrandVoiceProfile.cs
│   │   ├── ContentPillar.cs
│   │   └── HashtagGroup.cs
│   ├── Planning/
│   │   ├── EditorialCalendarPlanner.cs
│   │   └── EditorialSlot.cs
│   ├── Publishing/
│   │   ├── PublicationJob.cs
│   │   ├── PublicationJobStatus.cs
│   │   └── OrganicPublishRateLimitPolicy.cs
│   └── Traction/
│       ├── OrganicTractionDetector.cs
│       ├── PaidBoostSuggestion.cs
│       └── PaidBoostSuggestionStatus.cs
├── SocialPublishing.Application/
│   ├── SocialPublishing.Application.csproj
│   ├── DependencyInjection/
│   │   └── SocialPublishingApplicationServiceExtensions.cs
│   ├── Abstractions/
│   │   ├── IOrganicConnectionRepository.cs
│   │   ├── IOrganicPostRepository.cs
│   │   ├── IBrandVoiceRepository.cs
│   │   ├── IPublicationJobRepository.cs
│   │   ├── IPaidBoostSuggestionRepository.cs
│   │   ├── IOrganicPlatformAdapter.cs
│   │   ├── IOrganicOAuthStateService.cs
│   │   ├── IOrganicMediaStore.cs
│   │   ├── ISocialCreativeGenerator.cs
│   │   ├── IOrganicInsightsReader.cs
│   │   └── ISocialPublishingUnitOfWork.cs
│   ├── Connections/Commands/...                           # Start, Complete, Refresh, Revoke
│   ├── Connections/Queries/...
│   ├── Media/Commands/RegisterOrganicMedia/
│   ├── Brand/Commands/UpsertBrandVoice/
│   ├── Planning/Commands/GenerateEditorialPlan/
│   ├── Planning/Commands/GenerateSlotCopy/
│   ├── Posts/Commands/...                                 # Submit, Approve, Reject, Reschedule
│   ├── Posts/Queries/GetOrganicPosts/
│   ├── Publishing/Commands/DispatchDuePublications/
│   ├── Approvals/Commands/CreateApprovalLink/
│   ├── Approvals/Commands/DecidePublicApproval/
│   ├── Approvals/Queries/GetPublicApprovalBoard/
│   ├── Traction/Commands/ScanOrganicTraction/
│   ├── Traction/Commands/AcceptPaidBoostSuggestion/
│   └── Social/Queries/GetSocialModuleStatus/
└── SocialPublishing.Infrastructure/
    ├── SocialPublishing.Infrastructure.csproj             # Application + BuildingBlocks.Infrastructure
    ├── DependencyInjection/
    │   └── SocialPublishingInfrastructureServiceExtensions.cs
    ├── Persistence/
    │   ├── SocialPublishingDbContext.cs                   # fábrica: ITenantDbContextFactory<SocialPublishingDbContext>
    │   ├── SocialPublishingUnitOfWork.cs
    │   ├── Configurations/
    │   ├── Migrations/                                    # histórico social.__EFMigrationsHistory
    │   └── Repositories/
    ├── OAuth/
    │   ├── OrganicOAuthStateService.cs
    │   ├── OrganicTokenProtector.cs                       # IEncryptionService + propósito AES-256
    │   ├── MetaInstagramOrganicAdapter.cs
    │   ├── MetaFacebookPageOrganicAdapter.cs
    │   └── TikTokContentPostingAdapter.cs
    ├── Media/
    │   ├── LocalOrganicMediaStore.cs
    │   └── S3OrganicMediaStore.cs
    ├── Ai/
    │   └── HttpSocialCreativeGenerator.cs
    └── Insights/
        └── HttpOrganicInsightsReader.cs

src/Backend/WebApi/
├── Controllers/v1/
│   ├── SocialController.cs                                # rotas autenticadas /api/v1/social/...
│   └── PublicSocialApprovalsController.cs                 # /api/v1/public/social/approvals/{token}
├── Background/
│   └── SocialPublishingDispatchWorker.cs
└── Program.cs                                             # AddSocialPublishingModule + hosted services

src/Backend/Modules/Analytics/Analytics.Application/
└── Creatives/Notifications/
    └── LinkOrganicAssetWhenTractionDetected.cs            # só na Fase 6; referencia Contracts

src/Backend/Modules/Integrations/Integrations.Application/
└── Boost/Commands/CreatePaidBoostDraft/
    └── CreatePaidBoostDraftCommandHandler.cs              # handler do comando declarado em Contracts

src/Frontend/WebApp/
├── Services/Social/
│   ├── ISocialPublishingClient.cs
│   └── SocialPublishingClient.cs
├── Components/Social/
│   ├── SocialConnectionsPanel.razor
│   ├── SocialCalendarGrid.razor
│   ├── BrandVoiceForm.razor
│   ├── GeneratePlanDialog.razor
│   ├── BoostSuggestionBanner.razor
│   └── PublicApprovalPage.razor
├── Components/Shared/Components/
│   ├── OrganicPostStatusBadge.razor
│   └── ContentPillarChip.razor
├── Components/Pages/
│   ├── SocialConnectionsPage.razor                        # /social/connections
│   └── SocialCalendarPage.razor                           # /social/calendar
└── wwwroot/js/socialCalendar.js                           # só se o drag-and-drop HTML5 não fechar no circuito

tests/UnitTests/Backend/
├── Compliance/
│   ├── AgentsArchitectureComplianceTests.cs               # limites do novo módulo
│   └── XmlDocumentationComplianceTests.cs                 # assemblies SocialPublishing.*
└── SocialPublishing/
    ├── Domain/
    ├── Application/
    └── Publishing/SocialPublishingDispatchWorkerTests.cs

tests/UnitTests/Backend/Analytics/
└── LinkOrganicAssetWhenTractionDetectedTests.cs

tests/UnitTests/Backend/Integrations/
└── CreatePaidBoostDraftCommandHandlerTests.cs

tests/UnitTests/Frontend/Social/
├── SocialConnectionsPanelTests.cs
├── SocialCalendarGridTests.cs
├── PublicApprovalPageTests.cs
├── BrandVoiceFormTests.cs
└── BoostSuggestionBannerTests.cs

tests/IntegrationTests/SocialPublishing/
├── SocialPublishingDbContextTests.cs
└── PublicationJobClaimTests.cs

tests/AcceptanceTests/SocialPublishing/
├── SocialStatusEndpointTests.cs
└── PublicApprovalEndpointTests.cs
```

### Ordem de execução

A Fase N só começa com a Fase N−1 verde, incluindo o trecho correspondente de `docs/modules/social-publishing.md` e o ADR da fase. Dentro da fase, a ordem é a numérica das subfases. Dentro da subfase, a ordem é Red, depois Green, depois XML docs. O worker da Fase 4 não é registrado em `Program.cs` com `Enabled = true` por padrão; o primeiro valor seguro é desligado, como em `MetaSynchronizationWorker`.
