# ADR 0010: Frontend Corporativo em Blazor Server (.NET 10), Gestão de Estado de Sessão e Customização White-Label Dinâmica

## Status
Aceito

## Contexto
O AdMetricsPro é uma plataforma SaaS de gestão unificada de anúncios multitenant com isolamento por banco de dados. Para a camada de apresentação Web administrativa (painéis de clientes, squads e backoffice), o documento `AGENTS.md` (Seção 4) estabelece princípios inegociáveis:
1. **Blazor Server Interativo (.NET 10):** A interface opera no modo Server sob conexões SignalR estáveis, reduzindo overhead no cliente e garantindo execução com baixa latência e integração direta aos serviços da aplicação.
2. **Isolamento de Marca (White-Label):** Suporte nativo à customização visual de clientes corporativos (logomarca, cores primárias, secundárias, de destaque, favicon e supressão opcional de marcas institucionais).
3. **Gerenciamento de Estado por Circuito:** Propagação contextual segura da identidade do Tenant ativo para todos os componentes da árvore de renderização.
4. **TDD e Componentização Reutilizável:** Todo componente e contêiner de estado deve ser testável e desacoplado.

## Decisão
1. **Adoção do Blazor Web App no Modo InteractiveServer (`net10.0`):**
   - O projeto `WebApp` reside em `src/Frontend/WebApp/` e utiliza o SDK `Microsoft.NET.Sdk.Web` com `.AddInteractiveServerComponents()` e render mode interativo via SignalR.
   - Referencia diretamente os contratos do kernel compartilhado (`BuildingBlocks.Domain` e `BuildingBlocks.Application`), operando sobre envelopes `Result` e `Result<T>`.

2. **Provedor de Estado de Sessão (`ITenantStateProvider`):**
   - Registrado como serviço `Scoped` no container de injeção de dependências do ASP.NET Core, garantindo que cada circuito Blazor mantenha seu estado isolado em memória.
   - Fornece acesso ao `TenantState` e `TenantBranding`, com disparador de eventos `OnTenantChanged` para reatividade imediata em componentes inscritos.
   - Provê método `InitializeAsync(...)` para carregamento assíncrono do contexto na inicialização do circuito.

3. **Injeção Dinâmica de Tema White-Label via CSS Custom Properties:**
   - O modelo `TenantBranding` gera a string de estilo com variáveis CSS customizadas (`--tenant-primary`, `--tenant-secondary`, `--tenant-accent`).
   - O `MainLayout.razor` injeta essas variáveis diretamente no elemento raiz `.app-shell`, permitindo reestilização instantânea de componentes, botões, gráficos e bordas sem necessidade de compilação ou recarga completa da página.
   - O componente `AppFooter.razor` avalia a flag booleana `ShowPoweredBy`, omitindo o selo institucional caso o plano ou configuração do tenant determine a remoção da marca.

4. **Componentização Modular do Layout:**
   - `AppHeader`: barra superior com slots para o logotipo customizado do tenant, seletor/identificador de organização e perfil do usuário.
   - `AppSidebar`: menu lateral com navegação pelos módulos da plataforma e suporte responsivo (mobile drawer).
   - `AppFooter`: rodapé institucional dinâmico integrado ao White-Label.

## Consequências

### Positivas
- **Isolamento Estrito de Sessão:** Cada conexão SignalR possui sua própria instância de `TenantStateProvider`, eliminando vazamento de dados visuais entre diferentes abas ou usuários.
- **Reatividade Sem Fricção:** Alterações no branding do tenant propagam instantaneamente pelo evento `OnTenantChanged` e variáveis CSS.
- **Conformidade com TDD:** O contêiner de estado possui cobertura unitária automatizada prévia em `tests/UnitTests/Frontend/`.
- **Desempenho Otimizado:** Sem overhead de reprocessamento estático; o navegador apenas recalcula o CSS baseado nas variáveis dinâmicas.

### Negativas / Mitigações
- Manter conexões SignalR ativas consome memória por circuito no servidor. Mitigado pela arquitetura de escalabilidade horizontal do ASP.NET Core Blazor Server com Redis Backplane ou Azure SignalR Service quando necessário em produção.
