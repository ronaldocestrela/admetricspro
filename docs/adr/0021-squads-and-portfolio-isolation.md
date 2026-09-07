# ADR 0021: Gestão de Squads e Isolamento por Carteira (Portfolio Isolation)

## Status
Aceito

## Data
2026-09-07

## Contexto
No modelo operacional de agências de tráfego pago que utilizam o **AdMetricsPro**, analistas e gestores não devem ter acesso indiscriminado a todos os clientes gerenciados. Uma agência pode possuir células dedicadas a nichos concorrentes (ex: duas marcas rivais de moda atendidas por squads diferentes) ou equipes com níveis distintos de senioridade e confidencialidade.

Era imperativo estabelecer:
1. Um modelo flexível de agrupamento de equipes internas (`Squad`).
2. Alocação matricial de colaboradores (`TenantUser`) e clientes gerenciados (`Workspace`).
3. Uma regra estrita de autorização e visibilidade (Isolamento por Carteira / Portfolio Isolation).

## Decisão
1. **Agregado `Squad` no Banco do Inquilino:** Modelado como entidade rica e agregado raiz no `TenantDbContext`, contendo coleções privadas `_members` (`SquadMember`) e `_workspaces` (`SquadWorkspace`) expostas como somente-leitura.
2. **Relacionamento N:N:** Colaboradores podem participar de mais de um squad, e clientes podem receber atendimento multidisciplinar de mais de um squad.
3. **Isolamento de Carteira Hierárquico por RBAC (`UserPortfolioService`):**
   - Papéis com governança executiva (`TenantRole.Owner` e `TenantRole.Admin`) possuem acesso irrestrito a todos os workspaces da agência.
   - Papéis operacionais (`TenantRole.SquadLeader`, `TenantRole.MediaManager`, `TenantRole.Analyst`, `TenantRole.Guest`) têm sua visibilidade e permissões de operação restritas estritamente à união dos workspaces pertencentes aos squads ativos em que estão alocados.
   - Analistas sem squad recebem carteira vazia (acesso zero a clientes).
4. **Pattern `Result<T>` e Zero Exceções:** Todas as regras de negócio retornam `Result` com códigos semânticos estruturados.

## Consequências
- **Positivas:**
  - Garantia de governança e sigilo entre contas de clientes em agências de médio e grande porte.
  - Baixo acoplamento e facilidade de testes unitários através da abstração `IUserPortfolioService`.
  - Consultas rápidas com índices compostos e busca indexada por `UserId` e `WorkspaceId`.
- **Mitigações:**
  - Na deleção suave ou desativação de um Squad, as regras de portfolio passam a ignorá-lo imediatamente sem necessidade de apagar registros históricos.
