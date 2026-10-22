# Configurações completas dos recursos Meta

## Referência e alcance

Implementação baseada na API Graph **v26.0** e no SDK oficial **26.0.2**, commit `efd8423a2e595ea8d4c04eb824ce113f2f1d68cd`. O catálogo descreve a união dos parâmetros de criação e atualização: campanhas (34), conjuntos (76), anúncios (24) e criativos (66), total de 200 definições de raiz. Não são 200 opções disponíveis simultaneamente para toda conta. Permissões, país, objetivo, ativos, versão, tipo de compra e elegibilidade são confirmados pela Meta ao gravar.

Fonte primária: [SDK oficial da Meta](https://github.com/facebook/facebook-python-business-sdk/tree/efd8423a2e595ea8d4c04eb824ce113f2f1d68cd/facebook_business/adobjects). A matriz abaixo deriva de `AdAccount.create_campaign/create_ad_set/create_ad/create_ad_creative`, dos respectivos `api_update`, `_field_types` e enums. O catálogo não representa uma consulta de elegibilidade da conta.

## Como operar

Em **Gerenciamento de Anúncios → Meta → conta**, criar/editar campanha, conjunto e anúncio abre o editor ampliado. O criativo mantém o assistente de upload e também oferece **Configuração completa de criativo**. Pesquise por nome ou código técnico; as opções são agrupadas por assunto. Marque somente os campos a enviar. Na edição os campos começam desmarcados e os valores vêm da Meta. Campos não enviados permanecem intactos. Objetos enviados, como `targeting`, preservam os irmãos existentes até que o usuário os remova explicitamente.

Booleanos distinguem ausência e falso; orçamento distingue ausência e zero. BRL/USD/EUR/GBP são apresentados em moeda e convertidos para unidades mínimas; outras moedas mostram explicitamente as unidades mínimas da API. Datas são editadas em UTC. Listas com enum permitem seleção múltipla. Estruturas possuem campos recursivos até a profundidade catalogada e especificação JSON para opções especializadas ou estruturas sem definição de tipos no SDK. O editor não inventa controles de elegibilidade.

Campos somente de criação ficam identificados na edição. Campos reservados explicam o motivo. Um valor não retornado pela Meta fica identificado como indisponível, sem receber um padrão local. Falha na leitura principal impede salvar; falha em campo adicional não apaga os demais. O usuário pode recarregar campos individuais. JSON inválido bloqueia envio. Novos recursos são pausados; ativação posterior exige confirmação explícita. Erro de resultado incerto impede repetir automaticamente uma escrita.

## Análise por nível

### Campanha

Objetivos OUTCOME (reconhecimento, tráfego, engajamento, leads, aplicativo e vendas), tipo de compra, categorias especiais e países, status, limites de gastos, orçamento diário ou vitalício, estratégia de lance, programação, redistribuição e compartilhamento de orçamento, configurações Advantage+, objeto promovido e testes divididos conforme parâmetros abaixo. Tipo de compra e outras opções imutáveis exigem novo recurso. O tipo de compra é texto no SDK: usar o valor oficial compatível, por exemplo `AUCTION`; o backend não inventa um enum ausente na fonte.

A estratégia de orçamento é expressa pelos campos oficiais: no nível campanha para orçamento da campanha; nos conjuntos para orçamento por conjunto. `is_adset_budget_sharing_enabled` permite declarar explicitamente a opção quando a conta e a estratégia a suportam. Orçamento diário e vitalício positivos simultâneos são rejeitados localmente. Trocar a estratégia de uma campanha existente pode exigir zerar/remover o orçamento anterior conforme a operação aceita pela Meta; o editor não realiza migrações implícitas.

### Conjunto

Vínculo com campanha, orçamento/lances, evento de cobrança e otimização, atribuição, horários, frequência, ritmo de entrega, destino, pixel/evento/aplicativo/catálogo em `promoted_object`, segmentação e posicionamentos em `targeting`, públicos personalizados, inclusões/exclusões e requisitos regionais DSA. A busca consulta interesses, idiomas e países oficiais; páginas, pixels e públicos são carregados a partir da autorização da conta. Idade e gênero são restringidos em campanhas com categorias especiais. Regras regionais e combinações válidas continuam sujeitas à API.

### Anúncio e criativo

Anúncio controla vínculo com conjunto, criativo autorizado, status, rastreamento, domínio de conversão, prioridade, programação e demais parâmetros listados. O conteúdo é configurado no recurso criativo: publicação existente, link, imagem, vídeo, carrossel/estruturas de história, catálogo, conjunto de produtos, identidade Facebook/Instagram, asset feed, melhorias criativas, marcação de URL e formatos elegíveis. Criativos são majoritariamente imutáveis: alterar conteúdo exige novo criativo e associá-lo ao anúncio. O assistente existente continua responsável por upload/processamento de mídia; o catálogo não solicita caminhos de arquivo do servidor.

### Limites explícitos

O software não pode oferecer poderes superiores ao token e à API pública. Experimentos A/B completos (`AdStudy`), definições de segmentos do anunciante, revisão/publicação do app, portfólio e concessão de permissões são fluxos separados; não foram transformados em falsos interruptores de campanha. `iterative_split_test_configs` é exposto onde consta no SDK, mas não equivale ao produto completo Experimentos. Recursos `source_*` só aceitam origem da mesma conta vinculada. Campos de transporte, credenciais, caminhos internos e criação implícita de pais são bloqueados; use os fluxos dedicados para conta, upload, campanha e conjunto.

Não foi realizada escrita em anúncios reais para validar esta mudança. A cobertura local usa mocks da Graph e testes de componentes; a aceitação de cada combinação por uma conta real depende da resposta da Meta.

## Contratos HTTP

Todos exigem JWT, `X-Tenant-Id`, acesso ao workspace e conta interna vinculada. O frontend usa somente cliente HTTP tipado.

- `GET /api/v1/integrations/meta/resource-options?workspaceId={uuid}&accountId={uuid}&resource=campaigns`: catálogo autorizado, com versão e commit.
- `GET /api/v1/integrations/meta/resources/campaigns/{externalId}/configuration?workspaceId={uuid}&accountId={uuid}`: configuração atual; `fields=spend_cap,buying_type` limita a leitura. Recursos permitidos: `campaigns`, `adsets`, `ads`, `adcreatives`.
- `POST /api/v1/integrations/meta/resources`: escrita pelo diário idempotente existente. `externalId=null` cria; ID existente atualiza apenas `data`.

Exemplo de edição:

```json
{"operationId":"6dbd6afb-572f-4328-9192-412de0a02b1a","workspaceId":"4e6c9c8e-09fe-4071-af01-24e99b98f020","accountId":"e4a11e28-6289-46f0-9d5f-d847bf2c1228","resource":"campaigns","externalId":"123456789","data":{"daily_budget":15025},"confirmActivation":false}
```

Resposta de configuração, dentro do envelope Result padrão do controlador:

```json
{"isSuccess":true,"isFailure":false,"value":{"values":{"id":"123456789","name":"Vendas","daily_budget":"12000","is_adset_budget_sharing_enabled":false},"unavailableFields":{"spend_cap":"A Meta não retornou este campo para o objeto."}},"error":{"code":"","description":"","type":0}}
```

Falhas: `Meta.InvalidWrite` (tipo, enum, operação, credencial ou validação); `Meta.InvalidConfigurationFields` (campo de leitura inválido); `Meta.ConfigurationVersionMismatch` (backend diferente de v26.0); `Meta.ResourceAccountMismatch` (objeto, pai ou origem de outra conta); `Meta.AssetNotAuthorized`; `Meta.SpecialCategoryTargeting`; falhas de autorização/token e rejeições da Graph. HTTP usa 400/401/403/404/422 conforme o tipo da falha. Campos adicionais indisponíveis são retornados individualmente em `unavailableFields` quando a leitura principal teve sucesso.

## Regeneração

Baixe o arquivo oficial `https://codeload.github.com/facebook/facebook-python-business-sdk/zip/efd8423a2e595ea8d4c04eb824ce113f2f1d68cd` e execute:

```sh
python3 scripts/meta/build-resource-catalog.py SDK.zip src/Backend/Modules/Integrations/Integrations.Application/Meta/meta-resource-schema.json
```

O script analisa AST, não importa nem executa código do SDK, e verifica a raiz do arquivo correspondente ao commit. Atualizar versão exige revisar o catálogo, enums, documentação e testes; não há atualização silenciosa em produção.

## Matriz completa de parâmetros de raiz

Criar/Editar/Consultar indicam a presença no SDK; uma restrição local sempre prevalece. Consultar não garante retorno para qualquer conta. Subestruturas e enums completos estão no JSON versionado e no endpoint de opções.

### campaigns

| Campo | Nome | Tipo | Criar | Editar | Consultar | Opções e restrição |
|---|---|---|---|---|---|---|
| `adlabels` | Rótulos de organização | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `adset_bid_amounts` | Adset bid amounts | `map` | Não | Sim | Não | Conforme tipo e objeto oficial |
| `adset_budgets` | Adset budgets | `list<map>` | Não | Sim | Não | Conforme tipo e objeto oficial |
| `bid_strategy` | Estratégia de lance | `bid_strategy_enum` | Sim | Sim | Sim | COST_CAP, LOWEST_COST_WITHOUT_CAP, LOWEST_COST_WITH_BID_CAP, LOWEST_COST_WITH_MIN_ROAS |
| `budget_rebalance_flag` | Redistribuição de orçamento | `bool` | Não | Sim | Sim | Conforme tipo e objeto oficial |
| `budget_schedule_specs` | Programação de aumentos de orçamento | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `buying_type` | Tipo de compra | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `daily_budget` | Orçamento diário | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `execution_options` | Opções de execução da API | `list<execution_options_enum>` | Sim | Sim | Sim | include_recommendations, validate_only; BLOQUEADO: Reservado ao transporte. Este fluxo confirma gravações reais por diário idempotente; validate_only não é uma gravação. |
| `frequency_control_specs` | Controle de frequência | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_adset_budget_sharing_enabled` | Compartilhar orçamento entre conjuntos | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_budget_schedule_enabled` | Ativar programação de orçamento | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_direct_send_campaign` | Is direct send campaign | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_message_campaign` | Is message campaign | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_meta_moment_maker_enabled` | Is meta moment maker enabled | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_reels_trending_ads_enabled` | Is reels trending ads enabled | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_skadnetwork_attribution` | Atribuição SKAdNetwork | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `iterative_split_test_configs` | Configurações de teste dividido | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `lifetime_budget` | Orçamento vitalício | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `migrate_to_advantage_plus` | Migrar para Advantage+ | `bool` | Não | Sim | Não | Conforme tipo e objeto oficial |
| `name` | Nome | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `objective` | Objetivo da campanha | `objective_enum` | Sim | Sim | Sim | OUTCOME_APP_PROMOTION, OUTCOME_AWARENESS, OUTCOME_ENGAGEMENT, OUTCOME_LEADS, OUTCOME_SALES, OUTCOME_TRAFFIC |
| `pacing_type` | Ritmo de entrega | `list<string>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `promoted_object` | Objeto promovido: pixel, aplicativo ou catálogo | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `smart_promotion_type` | Tipo de promoção inteligente | `smart_promotion_type_enum` | Sim | Sim | Sim | GUIDED_CREATION, SMART_APP_PROMOTION |
| `source_campaign_id` | Campanha de origem | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `special_ad_categories` | Categorias de anúncio especiais | `list<special_ad_categories_enum>` | Sim | Sim | Sim | CREDIT, EMPLOYMENT, FINANCIAL_PRODUCTS_SERVICES, HOUSING, ISSUES_ELECTIONS_POLITICS, NONE, ONLINE_GAMBLING_AND_GAMING |
| `special_ad_category` | Categoria especial (legado) | `special_ad_category_enum` | Não | Sim | Sim | CREDIT, EMPLOYMENT, FINANCIAL_PRODUCTS_SERVICES, HOUSING, ISSUES_ELECTIONS_POLITICS, NONE, ONLINE_GAMBLING_AND_GAMING |
| `special_ad_category_country` | Países das categorias especiais | `list<special_ad_category_country_enum>` | Sim | Sim | Sim | AC, AD, AE, AF, AG, AI, AL, AM, AN, AO, AQ, AR, AS, AT, AU, AW, AX, AZ, BA, BB, BD, BE, BF, BG, BH, BI, BJ, BL, BM, BN, BO, BQ, BR, BS, BT, BV, BW, BY, BZ, CA, CC, CD, CF, CG, CH, CI, CK, CL, CM, CN, CO, CR, CU, CV, CW, CX, CY, CZ, DE, DJ, DK, DM, DO, DZ, EC, EE, EG, EH, ER, ES, ET, FI, FJ, FK, FM, FO, FR, GA, GB, GD, GE, GF, GG, GH, GI, GL, GM, GN, GP, GQ, GR, GS, GT, GU, GW, GY, HK, HM, HN, HR, HT, HU, ID, IE, IL, IM, IN, IO, IQ, IR, IS, IT, JE, JM, JO, JP, KE, KG, KH, KI, KM, KN, KP, KR, KW, KY, KZ, LA, LB, LC, LI, LK, LR, LS, LT, LU, LV, LY, MA, MC, MD, ME, MF, MG, MH, MK, ML, MM, MN, MO, MP, MQ, MR, MS, MT, MU, MV, MW, MX, MY, MZ, NA, NC, NE, NF, NG, NI, NL, NO, NP, NR, NU, NZ, OM, PA, PE, PF, PG, PH, PK, PL, PM, PN, PR, PS, PT, PW, PY, QA, RE, RO, RS, RU, RW, SA, SB, SC, SD, SE, SG, SH, SI, SJ, SK, SL, SM, SN, SO, SR, SS, ST, SV, SX, SY, SZ, TC, TD, TF, TG, TH, TJ, TK, TL, TM, TN, TO, TR, TT, TV, TW, TZ, UA, UG, UM, US, UY, UZ, VA, VC, VE, VG, VI, VN, VU, WF, WS, XK, YE, YT, ZA, ZM, ZW |
| `spend_cap` | Limite de gastos | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `start_time` | Início da veiculação | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `status` | Status configurado | `status_enum` | Sim | Sim | Sim | ACTIVE, ARCHIVED, DELETED, PAUSED |
| `stop_time` | Encerramento da campanha | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `topline_id` | Topline id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |

### adsets

| Campo | Nome | Tipo | Criar | Editar | Consultar | Opções e restrição |
|---|---|---|---|---|---|---|
| `account_id` | Conta proprietária | `string` | Não | Sim | Sim | Conforme tipo e objeto oficial; BLOQUEADO: A conta é fixada pelo vínculo autorizado do workspace; não pode ser transferida por este campo. |
| `ad_set_goal` | Ad set goal | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `adlabels` | Rótulos de organização | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `adset_schedule` | Programação por dias e horários | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `attribution_count_type` | Contagem de atribuição | `attribution_count_type_enum` | Sim | Não | Sim | ALL_CONVERSIONS, FIRST_CONVERSION |
| `attribution_spec` | Janelas e eventos de atribuição | `list<map>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `automatic_manual_state` | Automatic manual state | `automatic_manual_state_enum` | Sim | Sim | Sim | AUTOMATIC, MANUAL, UNSET |
| `bid_adjustments` | Bid adjustments | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `bid_amount` | Valor do lance | `int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `bid_constraints` | Restrições de lance | `map<string, Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `bid_strategy` | Estratégia de lance | `bid_strategy_enum` | Sim | Sim | Sim | COST_CAP, LOWEST_COST_WITHOUT_CAP, LOWEST_COST_WITH_BID_CAP, LOWEST_COST_WITH_MIN_ROAS |
| `billing_event` | Evento de cobrança | `billing_event_enum` | Sim | Sim | Sim | APP_INSTALLS, CLICKS, IMPRESSIONS, LINK_CLICKS, LISTING_INTERACTION, NONE, OFFER_CLAIMS, PAGE_LIKES, POST_ENGAGEMENT, PURCHASE, THRUPLAY |
| `brand_safety_config` | Brand safety config | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `budget_schedule_specs` | Programação de aumentos de orçamento | `list<Object>` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `budget_source` | Origem do orçamento | `budget_source_enum` | Sim | Não | Não | NONE, RMN |
| `budget_split_set_id` | Budget split set id | `string` | Sim | Não | Não | Conforme tipo e objeto oficial |
| `campaign_attribution` | Atribuição da campanha | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `campaign_id` | Campanha | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `campaign_spec` | Criação embutida de campanha | `Object` | Sim | Sim | Não | Conforme tipo e objeto oficial; BLOQUEADO: Crie a campanha no formulário dedicado e selecione seu ID; criação embutida não é utilizada. |
| `creative_sequence` | Creative sequence | `list<string>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `creative_sequence_repetition_pattern` | Creative sequence repetition pattern | `creative_sequence_repetition_pattern_enum` | Sim | Sim | Sim | FULL_SEQUENCE, LAST_AD |
| `daily_budget` | Orçamento diário | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `daily_imps` | Daily imps | `unsigned int` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `daily_min_spend_target` | Gasto mínimo diário | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `daily_spend_cap` | Limite diário de gastos | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `date_format` | Date format | `string` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `destination_type` | Tipo de destino | `destination_type_enum` | Sim | Sim | Sim | APP, APPLINKS_AUTOMATIC, FACEBOOK, FACEBOOK_LIVE, FACEBOOK_PAGE, IMAGINE, INSTAGRAM_DIRECT, INSTAGRAM_LIVE, INSTAGRAM_PROFILE, INSTAGRAM_PROFILE_AND_FACEBOOK_PAGE, MESSAGING_INSTAGRAM_DIRECT_MESSENGER, MESSAGING_INSTAGRAM_DIRECT_MESSENGER_WHATSAPP, MESSAGING_INSTAGRAM_DIRECT_WHATSAPP, MESSAGING_MESSENGER_WHATSAPP, MESSENGER, ON_AD, ON_EVENT, ON_PAGE, ON_POST, ON_VIDEO, SHOP_AUTOMATIC, WEBSITE, WHATSAPP |
| `dsa_beneficiary` | Beneficiário do anúncio (DSA) | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `dsa_payor` | Pagador do anúncio (DSA) | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `end_time` | Fim da veiculação | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `execution_options` | Opções de execução da API | `list<execution_options_enum>` | Sim | Sim | Não | include_recommendations, validate_only; BLOQUEADO: Reservado ao transporte. Este fluxo confirma gravações reais por diário idempotente; validate_only não é uma gravação. |
| `existing_customer_budget_percentage` | Orçamento para clientes existentes (%) | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `frequency_control_specs` | Controle de frequência | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `full_funnel_exploration_mode` | Full funnel exploration mode | `full_funnel_exploration_mode_enum` | Sim | Sim | Sim | EXTENDED_EXPLORATION, LIMITED_EXPLORATION, NONE_EXPLORATION |
| `is_ba_skip_delayed_eligible` | Is ba skip delayed eligible | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_budget_schedule_enabled` | Ativar programação de orçamento | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_dc_follow_optimized` | Is dc follow optimized | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_dynamic_creative` | Criativo dinâmico | `bool` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `is_incremental_attribution_enabled` | Atribuição incremental | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `is_sac_cfca_terms_certified` | Is sac cfca terms certified | `bool` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `is_sequenced_conversion_creation` | Is sequenced conversion creation | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `lifetime_budget` | Orçamento vitalício | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `lifetime_imps` | Lifetime imps | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `lifetime_min_spend_target` | Gasto mínimo vitalício | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `lifetime_spend_cap` | Limite vitalício de gastos | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `line_number` | Line number | `unsigned int` | Sim | Não | Não | Conforme tipo e objeto oficial |
| `live_video_ad_campaign_config` | Live video ad campaign config | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `max_budget_spend_percentage` | Participação máxima do orçamento (%) | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `meta_moment_maker_spec` | Meta moment maker spec | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `min_budget_spend_percentage` | Participação mínima do orçamento (%) | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `multi_event_conversion_attribution_window_seconds` | Multi event conversion attribution window seconds | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `multi_optimization_goal_weight` | Multi optimization goal weight | `multi_optimization_goal_weight_enum` | Sim | Sim | Sim | BALANCED, PREFER_EVENT, PREFER_INSTALL, UNDEFINED |
| `name` | Nome | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `optimization_goal` | Meta de otimização | `optimization_goal_enum` | Sim | Sim | Sim | ADVERTISER_SILOED_VALUE, AD_RECALL_LIFT, APP_INSTALLS, APP_INSTALLS_AND_OFFSITE_CONVERSIONS, AUTOMATIC_OBJECTIVE, CONVERSATIONS, DERIVED_EVENTS, ENGAGED_PAGE_VIEWS, ENGAGED_USERS, EVENT_RESPONSES, IMPRESSIONS, IN_APP_VALUE, LANDING_PAGE_VIEWS, LEAD_GENERATION, LINK_CLICKS, MEANINGFUL_CALL_ATTEMPT, MESSAGING_APPOINTMENT_CONVERSION, MESSAGING_DEEP_CONVERSATION_AND_FOLLOW, MESSAGING_PURCHASE_CONVERSION, NONE, OFFSITE_CONVERSIONS, PAGE_LIKES, POST_ENGAGEMENT, PROFILE_AND_PAGE_ENGAGEMENT, PROFILE_VISIT, QUALITY_CALL, QUALITY_LEAD, REACH, REMINDERS_SET, SUBSCRIBERS, THRUPLAY, VALUE, VISIT_INSTAGRAM_PROFILE |
| `optimization_sub_event` | Subevento de otimização | `optimization_sub_event_enum` | Sim | Sim | Sim | NONE, POST_INTERACTION, TRAVEL_INTENT, TRAVEL_INTENT_BUCKET_01, TRAVEL_INTENT_BUCKET_02, TRAVEL_INTENT_BUCKET_03, TRAVEL_INTENT_BUCKET_04, TRAVEL_INTENT_BUCKET_05, TRAVEL_INTENT_NO_DESTINATION_INTENT, TRIP_CONSIDERATION, VIDEO_SOUND_ON |
| `pacing_type` | Ritmo de entrega | `list<string>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `placement_soft_opt_out` | Placement soft opt out | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `promoted_object` | Objeto promovido: pixel, aplicativo ou catálogo | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `rb_prediction_id` | Rb prediction id | `string` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `regional_regulated_categories` | Categorias reguladas por região | `list<regional_regulated_categories_enum>` | Sim | Sim | Sim | 0, 1, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 2, 20, 21, 22, 24, 25, 3, 4, 5, 6, 7, 8, 9 |
| `regional_regulation_identities` | Identidades para regras regionais | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `relative_value` | Relative value | `float` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `rf_prediction_id` | Rf prediction id | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `source_adset_id` | Conjunto de origem | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `start_time` | Início da veiculação | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `status` | Status configurado | `status_enum` | Sim | Sim | Sim | ACTIVE, ARCHIVED, DELETED, PAUSED |
| `targeting` | Segmentação do público e posicionamentos | `Targeting` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `time_based_ad_rotation_id_blocks` | Time based ad rotation id blocks | `list<list<unsigned int>>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `time_based_ad_rotation_intervals` | Time based ad rotation intervals | `list<unsigned int>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `time_start` | Time start | `datetime` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `time_stop` | Time stop | `datetime` | Sim | Sim | Não | Conforme tipo e objeto oficial |
| `topline_id` | Topline id | `string` | Sim | Não | Não | Conforme tipo e objeto oficial |
| `trending_topics_spec` | Trending topics spec | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `tune_for_category` | Tune for category | `tune_for_category_enum` | Sim | Sim | Não | CREDIT, EMPLOYMENT, FINANCIAL_PRODUCTS_SERVICES, HOUSING, ISSUES_ELECTIONS_POLITICS, NONE, ONLINE_GAMBLING_AND_GAMING |
| `value_rule_set_id` | Conjunto de regras de valor | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `value_rules_applied` | Aplicar regras de valor | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |

### ads

| Campo | Nome | Tipo | Criar | Editar | Consultar | Opções e restrição |
|---|---|---|---|---|---|---|
| `ad_schedule_end_time` | Fim programado do anúncio | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `ad_schedule_start_time` | Início programado do anúncio | `datetime` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `adlabels` | Rótulos de organização | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `adset_id` | Conjunto de anúncios | `unsigned int` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `adset_spec` | Criação embutida de conjunto | `AdSet` | Sim | Sim | Sim | Conforme tipo e objeto oficial; BLOQUEADO: Crie o conjunto no formulário dedicado e selecione seu ID; criação embutida não é utilizada. |
| `audience_id` | Audience id | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `bid_amount` | Valor do lance | `int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `conversion_domain` | Domínio de conversão | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `creative` | Criativo do anúncio | `AdCreative` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `creative_asset_groups_spec` | Grupos de ativos do criativo | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `creative_audience_pairing_persona` | Combinação de criativo e perfil de público | `map` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `creative_automation_spec` | Automação do criativo | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `dataset_split_specs` | Divisão de conjuntos de dados | `list<map>` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `date_format` | Date format | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `display_sequence` | Ordem de exibição | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `draft_adgroup_id` | Draft adgroup id | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial; BLOQUEADO: Fluxo de rascunhos da Meta não implementado; utilize o anúncio pausado e seu ID confirmado. |
| `engagement_audience` | Engagement audience | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `execution_options` | Opções de execução da API | `list<execution_options_enum>` | Sim | Sim | Sim | include_recommendations, synchronous_ad_review, validate_only; BLOQUEADO: Reservado ao transporte. Este fluxo confirma gravações reais por diário idempotente; validate_only não é uma gravação. |
| `include_demolink_hashes` | Include demolink hashes | `bool` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `name` | Nome | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `priority` | Prioridade | `unsigned int` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `source_ad_id` | Anúncio de origem | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `status` | Status configurado | `status_enum` | Sim | Sim | Sim | ACTIVE, ARCHIVED, DELETED, PAUSED |
| `tracking_specs` | Rastreamento do anúncio | `Object` | Sim | Sim | Sim | Conforme tipo e objeto oficial |

### adcreatives

| Campo | Nome | Tipo | Criar | Editar | Consultar | Opções e restrição |
|---|---|---|---|---|---|---|
| `account_id` | Conta proprietária | `string` | Não | Sim | Sim | Conforme tipo e objeto oficial; BLOQUEADO: A conta é fixada pelo vínculo autorizado do workspace; não pode ser transferida por este campo. |
| `actor_id` | Actor id | `unsigned int` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `ad_disclaimer_spec` | Ad disclaimer spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `adlabels` | Rótulos de organização | `list<Object>` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `applink_treatment` | Applink treatment | `applink_treatment_enum` | Sim | Não | Sim | automatic, deeplink_with_appstore_fallback, deeplink_with_web_fallback, web_only |
| `asset_feed_spec` | Ativos dinâmicos: imagens, vídeos, textos e links | `Object` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `authorization_category` | Authorization category | `authorization_category_enum` | Sim | Não | Sim | NONE, POLITICAL, POLITICAL_WITH_DIGITALLY_CREATED_MEDIA |
| `body` | Texto principal | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `branded_content` | Branded content | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `branded_content_sponsor_page_id` | Branded content sponsor page id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `bundle_folder_id` | Bundle folder id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `call_to_action` | Chamada para ação | `Object` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `categorization_criteria` | Categorization criteria | `categorization_criteria_enum` | Sim | Não | Sim | brand, category, product_type |
| `category_media_source` | Category media source | `category_media_source_enum` | Sim | Não | Sim | CATEGORY, MIXED, PRODUCTS_COLLAGE, PRODUCTS_SLIDESHOW |
| `contextual_multi_ads` | Contextual multi ads | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `creative_sourcing_spec` | Creative sourcing spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `degrees_of_freedom_spec` | Aprimoramentos criativos Advantage+ | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `destination_set_id` | Destination set id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `destination_spec` | Destination spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `dynamic_ad_voice` | Dynamic ad voice | `dynamic_ad_voice_enum` | Sim | Não | Sim | DYNAMIC, STORY_OWNER |
| `enable_launch_instant_app` | Enable launch instant app | `bool` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `execution_options` | Opções de execução da API | `list<execution_options_enum>` | Sim | Não | Sim | validate_only; BLOQUEADO: Reservado ao transporte. Este fluxo confirma gravações reais por diário idempotente; validate_only não é uma gravação. |
| `existing_post_title` | Existing post title | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `facebook_branded_content` | Facebook branded content | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `format_transformation_spec` | Format transformation spec | `list<map>` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `generative_asset_spec` | Ativos criados com IA | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `image_crops` | Recortes da imagem | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `image_file` | Arquivo de imagem | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial; BLOQUEADO: Envie o arquivo pelo botão de mídia e utilize image_hash. Caminhos locais do servidor não são aceitos. |
| `image_hash` | Hash da imagem enviada | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `image_url` | URL da imagem | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `instagram_branded_content` | Instagram branded content | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `instagram_permalink_url` | Instagram permalink url | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `instagram_user_id` | Identidade do Instagram | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `interactive_components_spec` | Interactive components spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `is_dco_internal` | Is dco internal | `bool` | Sim | Não | Sim | Conforme tipo e objeto oficial; BLOQUEADO: Parâmetro interno da Meta, não apresentado como configuração pública garantida. |
| `link_og_id` | Link og id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `link_url` | URL do link | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `marketing_message_structured_spec` | Marketing message structured spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `media_optimization_spec` | Otimização de mídia | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `media_sourcing_spec` | Origem da mídia | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `name` | Nome | `string` | Sim | Sim | Sim | Conforme tipo e objeto oficial |
| `object_id` | Object id | `unsigned int` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `object_story_id` | Publicação existente | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `object_story_spec` | Página, identidade e conteúdo do criativo | `AdCreativeObjectStorySpec` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `object_type` | Object type | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `object_url` | Object url | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `omnichannel_link_spec` | Omnichannel link spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `page_welcome_message` | Page welcome message | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `place_page_set_id` | Place page set id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `platform_customizations` | Platform customizations | `Object` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `playable_asset_id` | Playable asset id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `portrait_customizations` | Portrait customizations | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `product_set_id` | Conjunto de produtos do catálogo | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `product_suggestion_settings` | Product suggestion settings | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `recommender_settings` | Recommender settings | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `regional_regulation_disclaimer_spec` | Regional regulation disclaimer spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `source_facebook_post_id` | Source facebook post id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `source_instagram_media_id` | Source instagram media id | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `status` | Status configurado | `status_enum` | Não | Sim | Sim | ACTIVE, DELETED, IN_PROCESS, WITH_ISSUES |
| `template_url` | Template url | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `template_url_spec` | Template url spec | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `thumbnail_url` | URL da miniatura | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `title` | Título | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `url_tags` | Parâmetros de rastreamento | `string` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `use_page_actor_override` | Use page actor override | `bool` | Sim | Não | Sim | Conforme tipo e objeto oficial |
| `wamo_whatsapp_identity_spec` | Wamo whatsapp identity spec | `map` | Sim | Não | Sim | Conforme tipo e objeto oficial |

