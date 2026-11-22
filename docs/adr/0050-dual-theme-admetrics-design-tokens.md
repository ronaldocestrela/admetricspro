# ADR 0050: Tema claro/escuro e tokens de cor AdMetricsPro

## Status

Aceito

## Contexto

O WebApp utilizava um tema escuro fixo com paleta heterogênea (slate/índigo/ciano) entre shell, login, onboarding e landing. A identidade do produto passou a adotar a proporção 60/30/10 (canvas, azul `#0131CD`, lima `#B9FC01`) com suporte a modo claro e escuro, mantendo white-label apenas nas cores primária e de acento do tenant.

## Decisão

1. Centralizar tokens em [`theme.css`](../../src/Frontend/WebApp/wwwroot/css/theme.css) com prefixo `--ad-*` no `:root` (claro) e em `[data-theme="dark"]` (escuro).
2. Persistir preferência do usuário em `localStorage` (`admetrics-theme`) e aplicar `data-theme` no elemento `<html>` via script anti-FOUC em `App.razor` e módulo [`theme.js`](../../src/Frontend/WebApp/wwwroot/js/theme.js).
3. Manter aliases legados (`--bg-app`, `--text-primary`, etc.) mapeados aos tokens para compatibilidade com CSS existente.
4. No shell autenticado e no login, `--ad-primary-brand` e `--ad-accent-volt` derivam de `--tenant-primary` e `--tenant-accent` injetados por `TenantBranding.ToCssVariables()`; canvas, texto e bordas seguem o tema global.
5. Expor alternância de tema via componente `ThemeToggle` no header do tenant, login, onboarding e landing.

## Consequências

- **Positivas:** consistência visual, acessibilidade (modo claro padrão), white-label previsível, menos hex duplicados nos componentes.
- **Negativas:** migração gradual de CSS isolado com cores hardcoded; partículas da landing dependem de leitura de variáveis CSS em runtime.
- **Testes:** `ThemePreferenceResolver` coberto por testes unitários; defaults de `TenantBranding` atualizados para `#0131CD` / `#B9FC01`.