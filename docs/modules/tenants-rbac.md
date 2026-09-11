# Módulo Tenants — Matriz de Perfis e Permissões (RBAC Granular) & Auditoria Imutável

Este documento descreve as diretrizes arquiteturais, a matriz de permissões canônicas, as regras de avaliação contextual (governança de carteira, teto orçamentário e blindagem de cliente final), os contratos da Web API e o modelo de auditoria imutável (`TenantAuditLog`) no banco de dados dedicado por inquilino (`TenantDbContext`).

---

## 1. Visão Geral e Arquitetura

O controle de acesso baseado em funções (**Role-Based Access Control - RBAC**) no **AdMetricsPro** estabelece privilégios granulares para cada membro da agência e observadores externos, operando de forma desacoplada através do avaliador `IPermissionEvaluator` e protegido no pipeline in-memory via `TenantAuthorizationBehavior` (MediatR).

### 1.1 Entidades e Papéis Operacionais (`TenantRole`)
1. **Owner (Proprietário da Agência):** Acesso irrestrito total, incluindo gerenciamento de planos, faturamento raiz (`ManageBilling`) e auditoria.
2. **Admin (Administrador da Agência):** Gestão global da agência (squads, clientes, integrações, configurações e auditoria), exceto titularidade/faturamento raiz.
3. **SquadLeader (Líder de Squad):** Gestão operacional restrita aos clientes e membros alocados aos seus squads ativos.
4. **MediaManager (Gestor de Mídia):** Criação, edição e ajustes de campanhas nos workspaces de seus squads, respeitando o **teto de segurança orçamentário** configurado.
5. **Analyst (Analista de Performance / BI):** Acesso estritamente de leitura de métricas, dashboards e relatórios dentro de sua carteira (sem privilégios de edição ou visualização de margens financeiras da agência).
6. **Guest (Client Guest / Cliente Final):** Visualização blindada **apenas** de seu próprio Workspace; bloqueio estrito contra markups, fees de agência, margens de lucro, regras internas e dados de outros clientes.

---

## 2. Matriz Canônica de Permissões (`TenantPermission`)

| Permissão (`TenantPermission`) | Owner | Admin | SquadLeader | MediaManager | Analyst | Guest (Cliente Final) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| `ManageBilling` | Sim | Não | Não | Não | Não | **BLOCKED** |
| `ManageSettings` | Sim | Sim | Não | Não | Não | **BLOCKED** |
| `ViewAuditLogs` | Sim | Sim | Não | Não | Não | **BLOCKED** |
| `ManageTeamMembers` | Sim | Sim | Apenas Squad | Não | Não | **BLOCKED** |
| `ManageSquads` | Sim | Sim | Apenas Squad | Não | Não | **BLOCKED** |
| `ManageWorkspaces` | Sim | Sim | Apenas Squad | Não | Não | **BLOCKED** |
| `ViewCampaigns` | Sim | Sim | Na Carteira | Na Carteira | Na Carteira | Apenas Próprio |
| `EditCampaigns` | Sim | Sim | Na Carteira | Na Carteira (com Teto) | Não | **BLOCKED** |
| `ManageAutomations` | Sim | Sim | Na Carteira | Na Carteira | Não | **BLOCKED** |
| `ViewFinancialMargins` | Sim | Sim | Sim | Não | Não | **BLOCKED ESTRITO** |
| `ExportReports` | Sim | Sim | Na Carteira | Na Carteira | Na Carteira | Apenas Próprio |

---

## 3. Regras Especiais de Governança

### 3.1 Blindagem de Cliente Final (Guest)
- Clientes convidados como `Guest` possuem acesso exclusivo ao seu Workspace alocado.
- A permissão `ViewFinancialMargins` (margens de agência, markup e taxas de serviço) é **rigorosamente bloqueada**, tanto no backend (`TenantPermissionEvaluator`) quanto no frontend através do componente de renderização condicional `<TenantAuthorizeView Permission="TenantPermission.ViewFinancialMargins">`.
- Tentativas de acessar dados de outros workspaces resultam em `Result.Failure(Error.Forbidden)`.

### 3.2 Teto de Segurança Orçamentário para Gestor de Mídia
- O Gestor de Mídia pode criar e editar campanhas até um teto configurável (`_maxBudgetLimitForMediaManager`, padrão R$ 50.000,00).
- Qualquer comando de alteração de orçamento cujo valor exceda o teto configurado é automaticamente negado pelo `IPermissionEvaluator`.

---

## 4. Trilha de Auditoria Imutável (`TenantAuditLog`)

Todas as operações críticas de governança, incluindo alterações de papéis de colaboradores (`User.RoleChanged`) e promoções operacionais, são gravadas na tabela `TenantAuditLogs` no banco dedicado do inquilino.

### Estrutura do Registro:
- **`Id`:** Identificador único (GUID).
- **`UserId`:** Identificador do operador responsável.
- **`UserEmail`:** Email do operador no momento da ação.
- **`Action`:** Identificador da ação (ex: `User.RoleChanged`).
- **`Resource`:** Recurso impactado (ex: `TenantUser`).
- **`ResourceId`:** Identificador do recurso impactado.
- **`Details`:** Descrição contextual da operação.
- **`IpAddress`:** Endereço IP do operador.
- **`CreatedAtUtc`:** Timestamp UTC imutável de registro.

---

## 5. Endpoints da Web API (`/api/v1/tenants`)

### 5.1 Obter Matriz de Permissões
- **Rota:** `GET /api/v1/tenants/rbac/matrix`
- **Sumário:** Retorna a matriz canônica de papéis e permissões granulares do inquilino.
- **Status:** `200 OK`

#### Exemplo de Resposta (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "roles": {
      "Owner": ["ManageBilling", "ManageSettings", "ViewAuditLogs", "ManageTeamMembers", "ManageSquads", "ManageWorkspaces", "ViewCampaigns", "EditCampaigns", "ManageAutomations", "ViewFinancialMargins", "ExportReports"],
      "Admin": ["ManageSettings", "ViewAuditLogs", "ManageTeamMembers", "ManageSquads", "ManageWorkspaces", "ViewCampaigns", "EditCampaigns", "ManageAutomations", "ViewFinancialMargins", "ExportReports"],
      "SquadLeader": ["ManageTeamMembers", "ManageSquads", "ManageWorkspaces", "ViewCampaigns", "EditCampaigns", "ManageAutomations", "ViewFinancialMargins", "ExportReports"],
      "MediaManager": ["ViewCampaigns", "EditCampaigns", "ManageAutomations", "ExportReports"],
      "Analyst": ["ViewCampaigns", "ExportReports"],
      "Guest": ["ViewCampaigns", "ExportReports"]
    },
    "allPermissions": ["ManageBilling", "ManageSettings", "ViewAuditLogs", "ManageTeamMembers", "ManageSquads", "ManageWorkspaces", "ViewCampaigns", "EditCampaigns", "ManageAutomations", "ViewFinancialMargins", "ExportReports"]
  }
}
```

### 5.2 Alterar Papel de Colaborador (com Auditoria)
- **Rota:** `PUT /api/v1/tenants/users/{id}/role`
- **Sumário:** Altera o papel funcional de um colaborador com gravação de auditoria imutável.
- **Status:** `200 OK`, `400 BadRequest`, `404 NotFound`

#### Payload de Requisição:
```json
{
  "newRole": 3
}
```

### 5.3 Consultar Trilha de Auditoria
- **Rota:** `GET /api/v1/tenants/audit-logs?action=User.RoleChanged&page=1&pageSize=20`
- **Sumário:** Lista a trilha de auditoria operacional do inquilino.
- **Status:** `200 OK`

---

## 6. Catálogo de Erros de Negócio

| Código do Erro | Descrição | HTTP Status |
| :--- | :--- | :---: |
| `TenantUser.NotFound` | Colaborador não localizado no inquilino. | `404 NotFound` |
| `TenantUser.Inactive` | Colaborador inativo (operações bloqueadas). | `403 Forbidden` |
| `Auth.Unauthorized` | Operação requer autenticação de usuário no inquilino. | `401 Unauthorized` |
| `Auth.Forbidden` | Usuário não possui a permissão necessária para executar a ação. | `403 Forbidden` |
| `TenantAuditLog.InvalidId` | Identificador do log de auditoria não pode ser vazio. | `400 BadRequest` |
