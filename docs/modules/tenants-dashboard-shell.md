# Área autenticada do cliente: layout, tema e navegação

## Objetivo e composição

A área do cliente segue a identidade da landing page: fundo escuro, superfícies discretas, tipografia Inter, bordas suaves e destaques na cor da marca. A composição e as ações das telas existentes foram preservadas; o cabeçalho superior duplicado foi removido e suas ações passaram para o menu lateral.

| Componente | Responsabilidade |
| --- | --- |
| `TenantMainLayout` | Aplica o tema no container `.tenant-app-shell`, renderiza sidebar, banner de avaliação, conteúdo e controle móvel de abertura. |
| `TenantSidebar` | Marca com link para `/dashboard`, seleção global de cliente, navegação, identificação do usuário e saída da conta. |
| `TenantTrialBanner` | Mantém a informação do período de avaliação e o acesso a `/settings/billing`. |
| `TenantComposedName` | Destaca, na cor primária, o trecho após a primeira palavra de nomes compostos. Para AdMetricsPro, destaca Pro. |

`TenantTopHeader` permanece no repositório por compatibilidade, mas não é renderizado nesse layout. Em desktop, a barra lateral tem 284 px e acompanha a tela. Em telas de até 1024 px funciona como gaveta com overlay, fechamento por botão, Escape ou seleção de uma rota. O conteúdo conserva seus próprios títulos.

## Tema e identidade do cliente

As cores explicitamente configuradas pelo cliente têm prioridade. Somente cores ausentes ou vazias recebem os padrões da LP:

```css
--tenant-primary-color: #4E8EFF;
--tenant-secondary-color: #090E19;
--tenant-accent-color: #4BD8EC;
```

`TenantBranding.ToCssVariables()` também publica os aliases `--tenant-primary`, `--tenant-secondary` e `--tenant-accent`. A primária colore títulos compostos nas páginas, controles e botões. Textos do menu lateral mantêm cor neutra, inclusive nomes de marca e usuário; o item ativo conserva seu indicador visual. O banner de avaliação mantém amarelo no selo, borda e botão para facilitar a identificação. A secundária participa da composição das superfícies; a cor de destaque participa dos gradientes. Cores semânticas de sucesso, alerta, falha e identificação das plataformas são preservadas. Logo cadastrada é mantida; nomes sem imagem usam monograma. A identidade padrão utiliza `/images/landing/brand-mark.svg`.

`wwwroot/css/tenant-workspace.css` concentra tokens, formulários, botões, tabelas, estados vazios, alertas, modais e utilitários necessários às telas existentes. Todos os seletores são limitados a `.tenant-app-shell`, evitando interferência na LP, autenticação, onboarding ou backoffice. CSS isolado dos componentes utiliza os mesmos tokens. Ícones de classes existentes usam máscaras SVG locais.

## Cliente ativo e encerramento da sessão

O seletor consulta `IWorkspaceClientService.GetWorkspacesAsync(activeOnly: true)` e permite selecionar um cliente ou todos os clientes. O contexto continua em `IWorkspaceContextStateProvider`, com notificação das telas assinantes e persistência protegida no navegador. Durante carregamento o seletor fica desabilitado; falhas mostram mensagem contextual. Nenhuma consulta a banco ocorre no frontend.

A ação de sair fica no rodapé do menu lateral, desabilitada enquanto executa. Ela aguarda `WorkspaceContextStateProvider.ClearAsync()` e `TenantSessionStateProvider.ClearSessionAsync()`, removendo contexto e sessão persistidos antes de navegar para `/login`. A remoção é assíncrona para evitar bloqueio do circuito Blazor por espera síncrona em interoperabilidade com o navegador. Ausência de armazenamento disponível mantém o tratamento gracioso existente.

## Telas e contratos HTTP revisados

Os clientes HTTP e os contratos de negócio existentes permanecem em uso. Não foram criados endpoints, modificados payloads ou substituído o padrão `Result<T>`.

| Tela | Endpoints versionados consumidos |
| --- | --- |
| `/dashboard` | GET `/api/v1/analytics/dashboard/executive`, GET `/api/v1/automations/pacing/portfolio`, GET `/api/v1/automations/pacing/workspaces/{id}`, GET `/api/v1/tenants/ftux-status`. |
| `/workspaces` | GET/POST `/api/v1/workspaces`, GET/PUT `/api/v1/workspaces/{id}`, PATCH `/api/v1/workspaces/{id}/toggle-status`. |
| `/squads` | GET/POST `/api/v1/squads`, GET/PUT `/api/v1/squads/{id}`, PATCH `/api/v1/squads/{id}/status`, associação e remoção em `/{id}/members` e `/{id}/workspaces`, GET `/api/v1/squads/users/{id}/portfolio`, GET `/api/v1/tenants/users`. |
| `/integrations` | `/api/v1/integrations/oauth/authorize-url`, `/status/{workspaceId}`, `/callback`, `/refresh` e exclusão em `/{workspaceId}/{platform}`. |
| `/copilot` | GET `/api/v1/analytics/copilot/diagnostic`, POST `/api/v1/analytics/copilot/actions/execute`. |
| `/creatives` | GET `/api/v1/analytics/creatives/overview`, `/fatigue` e `/comparison`. |
| `/campaigns/bulk` | GET `/api/v1/integrations/campaigns`, POST `/api/v1/integrations/campaigns/bulk`. |
| `/reports` | CRUD `/api/v1/workspaces/{id}/reports/schedules`, POST `/reports/generate`, POST `/reports/schedules/{id}/dispatch`, GET `/reports/history`, GET `/reports/{id}/pdf`, dentro do mesmo workspace. |
| `/rbac` | GET `/api/v1/tenants/rbac/matrix`, GET `/api/v1/tenants/rbac/users/{id}/permissions`, PUT `/api/v1/tenants/users/{id}/role`, GET `/api/v1/tenants/audit-logs`. |
| `/settings/white-label` | GET/PUT `/api/v1/tenants/branding`, GET/PUT/DELETE `/api/v1/tenants/cname`. |
| `/settings/billing` | GET `/api/v1/billing/checkout/preview`, POST `/api/v1/billing/checkout`, GET `/api/v1/billing/checkout/{transactionId}/status`. |

Filtros, tabelas, gráficos, permissões, integrações, edição de campanhas e contratação mantêm seus comportamentos e limites existentes. Esta mudança é de apresentação e organização do shell; não muda a arquitetura de acesso à API.

## Validação

Testes bUnit foram escritos antes das alterações funcionais para verificar ausência do cabeçalho duplicado, controles no menu, contexto global, saída assíncrona, cores personalizadas, fallback de cores vazias e destaque dos nomes. O cenário de armazenamento pendente reproduziu o bloqueio no logout antes da correção.

A suíte completa do frontend passou com 289 testes. A inspeção em navegador percorreu 11 telas com dados HTTP simulados e uma marca personalizada, verificou seleção do cliente e logout e avaliou larguras de 1440, 1024, 390 e 320 px. Não houve exceções JavaScript nem transbordamento horizontal da página. As verificações visuais utilizaram fixtures locais, sem escrita nos dados reais do cliente; integrações externas e pagamento não foram executados.
