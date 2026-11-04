# 0047 — Portal white-label de aprovação orgânica

## Status

Aceito em 2026-11-04. Subfase **5.3** implementada (link compartilhável, rotas públicas anônimas, composição de branding na Web API e página Blazor em `EmptyLayout`).

## Contexto

Clientes finais das agências precisam aprovar posts orgânicos sem conta na AdMetricsPro, com identidade visual da agência (ADR 0025, precedente de relatórios públicos ADR 0037). O bounded context `SocialPublishing` não pode ler `TenantBranding` diretamente (ADR 0043). Tokens de acesso não podem ser persistidos em claro.

## Decisão

1. **Agregado `ApprovalLink`** no schema `social`: um pacote ativo por `WorkspaceId`, hash SHA-256 (64 hex) do token, expiração obrigatória, revogação ao emitir novo link.

2. **Token opaco:** 32 bytes (`RandomNumberGenerator`), entrega única em Base64Url na resposta autenticada `POST /api/v1/social/approval-links`. Comparação sempre por hash (`ApprovalTokenHasher`).

3. **Quadro público:** `GetPublicApprovalBoardQuery` lista posts `PendingApproval` do workspace do link válido, sem branding. Falhas de token (ausente, expirado, revogado) usam `Social.ApprovalTokenInvalid` (`Error.NotFound`).

4. **Decisão pública:** `DecidePublicApprovalCommand` aprova ou solicita alterações (`ClientApprovalComment` em `OrganicPost`). Posts fora do workspace → `Social.PostNotFound`.

5. **Composição na Web API:** `PublicSocialApprovalsController` (`[AllowAnonymous]`) monta `PublicApprovalBoardDto` com `GetTenantBrandingQuery` + `GetTenantPublicBrandingQuery` (nome da agência). Respostas 404 normalizadas para não distinguir motivo do token inválido.

6. **Resolução de tenant:** middleware existente + header `X-Tenant-Id` (slug em `/t/{slug}/approve/{token}`) + **`ForwardedHostTenantIdentificationStrategy`** (`X-Forwarded-Host` com host do browser no Blazor → API).

7. **Frontend:** `PublicApprovalPage` em `EmptyLayout`, rotas `/approve/{token}` e `/t/{slug}/approve/{token}`; clientes `IPublicSocialApprovalClient` e `CreateApprovalLinkAsync` no calendário autenticado.

## Consequências

### Positivas

- Aprovação externa alinhada ao white-label sem acoplar SocialPublishing a Tenants.Infrastructure.
- Mesma superfície de erro para enumeração de tokens.
- Testes unitários, aceitação e bUnit cobrindo hash, revogação, 404 homogêneo e UI pública.

### Negativas / Mitigações

- Dois round-trips MediatR para branding no GET público — aceitável para volume baixo de aprovação.
- Dependência de headers corretos quando Blazor e API estão em hosts distintos — documentado na spec e no cliente (`X-Forwarded-Host`).
