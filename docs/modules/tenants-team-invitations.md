# Convites permanentes de colaboradores

## Caso de uso

Administradores (Owner/Admin) convidam colaboradores pela tela `/settings/team` ou pelo modal FTUX. O sistema persiste o usuário **inativo**, registra um convite com token opaco (hash SHA-256) e envia e-mail com link para `/convite/{token}?subdomain={slug}`.

O colaborador define a senha na página pública; a API ativa a conta e libera login em `/login`.

## Endpoints autenticados

### POST `/api/v1/tenants/users`

**Entrada**

```json
{
  "fullName": "Carlos Oliveira",
  "email": "carlos@suaagencia.com.br",
  "role": "MediaManager",
  "phoneNumber": "11999990000"
}
```

**Retorno (`Result<InviteTenantUserResultDto>`)**

```json
{
  "isSuccess": true,
  "value": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "emailDispatched": true
  }
}
```

| Código de erro | HTTP |
| --- | --- |
| `TenantUser.EmailAlreadyInUse` | 409 |
| `TenantUser.OwnerNotAssignable` | 400 |
| `TenantUser.InviteForbidden` | 403 |
| `Auth.Forbidden` (sem permissão RBAC) | 403 |

### POST `/api/v1/tenants/users/{id}/invitations`

Reenvia convite para colaborador com status `PendingAcceptance`. Mesmo DTO de retorno do POST de convite.

### GET `/api/v1/tenants/users`

Lista colaboradores; `tenantUserDto.accountStatus` ∈ `Active`, `PendingAcceptance`, `Inactive`.

## Endpoints públicos

### GET `/api/v1/public/tenant-invitations/{token}?subdomain=vanguarda`

Retorna `TenantInvitationPreviewDto` (nome, e-mail, agência, expiração).

### POST `/api/v1/public/tenant-invitations/accept`

```json
{
  "token": "opaque-token",
  "subdomain": "vanguarda",
  "password": "SenhaForte@9",
  "confirmPassword": "SenhaForte@9"
}
```

Erros comuns: `TenantInvitation.NotFound`, `TenantUser.WeakPassword`, `TenantUser.PasswordMismatch`, `TenantUser.AlreadyActive`.

## Configuração

Convites exigem **URL base do WebApp** (montagem do link no e-mail) e provedor de e-mail **Smtp** para entrega real (ver [mensageria transacional](transactional-emails-and-trial-notices.md)).

`appsettings` ou variáveis `.env`:

```json
"Tenants": {
  "Invitations": {
    "WebAppBaseUrl": "https://app.exemplo.com",
    "TokenValidityHours": 72
  }
}
```

Equivalente no `.env`:

```bash
Tenants__Invitations__WebAppBaseUrl=https://app.exemplo.com
Tenants__Invitations__TokenValidityHours=72
Email__Provider=Smtp
```

Se `WebAppBaseUrl` estiver ausente, o convite é persistido mas `emailDispatched` retorna `false` (`TenantInvitation.WebAppBaseUrlMissing`). Com `Email__Provider=InMemory`, a API pode marcar envio bem-sucedido sem SMTP real (apenas testes/desenvolvimento).

No Docker, o arquivo `.env` não entra na imagem (`.dockerignore`). O `--env-file` do Compose só substitui `${VAR}` declaradas em `environment` do serviço `webapi`. `Email__*` e `Tenants__Invitations__*` precisam estar nessa lista; caso contrário o `appsettings.json` publicado (`Provider: InMemory`) permanece ativo. A URL do link deve ser a do portal publicado (padrão `http://localhost:7000`).