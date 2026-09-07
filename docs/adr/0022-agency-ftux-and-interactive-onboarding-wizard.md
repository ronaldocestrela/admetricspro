# ADR 0022: Wizard de Primeiro Acesso da Agência (FTUX) e Dados Demonstrativos

## Status
Aceito

## Data
2026-09-07

## Contexto
Após o provisionamento do ambiente isolado e a criação do usuário *Owner*, agências de tráfego pago recém-cadastradas enfrentavam um vazio operacional ("cold start"):
1. Não havia dados de tráfego para visualizar, criando uma sensação de sistema estático ou incompleto.
2. A integração oficial via OAuth2 com redes como Meta Ads e Google Ads pode depender de aprovação externa de aplicativos corporativos, o que poderia bloquear a ativação imediata da agência na plataforma.
3. Era necessário guiar o usuário de forma prescritiva nas 4 configurações essenciais (provisionamento, 1º cliente, conexão de mídia e estruturação de equipe).

## Decisão
1. **Checklist Interativo com Progresso Dinâmico (`AgencyFtuxChecklist`):**
   - Implementado no topo da tela de visão geral (`/dashboard`), computando 25% por etapa fundamental concluída.
   - Passo 1 (25%): Ambiente e banco SQL Server dedicados provisionados (sempre ativo no tenant operacional).
   - Passo 2 (25%): Cadastro do 1º Cliente (`Workspace`).
   - Passo 3 (25%): Conexão de conta de anúncios (`ConnectedAdAccount`), seja real via OAuth2 ou em modo demonstração.
   - Passo 4 (25%): Estruturação de time (convite de colaborador ou criação do 1º Squad).
2. **Modo Demonstração Instantâneo para Aceleração do FTUX:**
   - Adicionada a capacidade de instanciar contas de anúncios em modo demonstração (`IsDemo = true`) através do comando `ConnectDemoAdAccountCommand`.
   - Permite que a agência popule seu dashboard com métricas sintéticas e explore relatórios imediatamente sem bloqueios de homologação externa de terceiros.
3. **Frontend Estritamente de Apresentação via Web API (Regra 9 do AGENTS.md):**
   - O componente Blazor Server interage exclusivamente através de clientes HTTP fortemente tipados (`ITenantFtuxClientService`, `IWorkspaceClientService`, `ITenantTeamClientService`), tratando envelopes `Result<T>` e repassando o contexto do inquilino via header HTTP `X-Tenant-Id`.
4. **Modais de Ação Rápida no Circuito Blazor:**
   - Modais acessíveis e reativos (`WorkspaceQuickModal`, `AdConnectionQuickModal`, `TeamQuickModal`) integrados diretamente no circuito SignalR com atualização instantânea do checklist.

## Consequências
- **Positivas:**
  - Redução drástica do Time-to-Value (TTV) para novos gestores e donos de agência.
  - Zero atrito no primeiro acesso: a agência pode testar todas as funcionalidades do sistema com dados simulados em menos de 1 minuto.
  - Total conformidade com a arquitetura modular, isolamento de banco e regras de documentação e TDD.
- **Mitigações:**
  - Contas demonstrativas possuem flag persistida `IsDemo = true` no banco dedicado do inquilino, permitindo que sejam filtradas, substituídas ou excluídas quando a agência vincular contas reais de clientes.
