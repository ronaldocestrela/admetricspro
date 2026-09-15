# ADR 0026: Hub de Integrações OAuth2 e Criptografia AES-256 no Token Vault

## Status
Aceito

## Data
2026-09-15

## Contexto
O SaaS **AdMetricsPro** unifica o monitoramento e otimização de anúncios em 4 grandes plataformas de mídia externa: **Meta Ads**, **Google Ads**, **Bing Ads** e **TikTok Ads**.
O acesso a essas redes exige fluxos de autorização baseados em OAuth 2.0. As credenciais resultantes (`access_token` e `refresh_token`) são segredos críticos que concedem permissão de leitura de métricas, gestão de orçamentos e pausa de campanhas em contas reais dos clientes das agências.

Requisitos mandatórios:
1. **Isolamento de Segurança e Multitenancy:** As credenciais devem pertencer exclusivamente ao workspace do inquilino e ser armazenadas no seu banco de dados dedicado (`TenantDbContext`).
2. **Criptografia em Repouso Estrita:** Tokens nunca podem ser persistidos em texto claro no banco de dados.
3. **Mitigação Rigorosa de Ataques CSRF:** O fluxo OAuth2 exige verificação de integridade entre a requisição inicial e o retorno do callback.
4. **Renovação Preventiva de Tokens:** Muitas APIs (como Meta com validade de 60 dias e Google com expiração de 1 hora) necessitam de atualização preventiva para que jobs de sincronização não falhem em lote.
5. **Zero Acesso Direto a Banco no Frontend:** Todas as operações de conexão devem ser expostas exclusivamente através da Web API e orquestradas por Handlers CQRS com retornos tipados via `Result<T>`.

## Decisão

### 1. Criação do Módulo Autônomo `Integrations`
O módulo foi estruturado em três camadas sob o princípio de Monólito Modular do .NET 10:
- **`Integrations.Domain`:** Define contratos de adaptadores (`IOAuthAdapter`), orquestrador unificado (`IAdNetworkAuthService`), repositórios (`IOAuthTokenVaultRepository`), serviços de estado anti-CSRF (`IOAuthStateService`) e de criptografia (`IOAuthEncryptionService`).
- **`Integrations.Application`:** Handlers CQRS para início de fluxo, callback, renovação em lote e revogação, além de opções fortemente tipadas (`OAuthNetworkOptions`).
- **`Integrations.Infrastructure`:** Implementação dos adaptadores HTTP para as 4 redes externas com typed `HttpClient`, repositório EF Core para persistência no `TenantDbContext` e criptografia simétrica com AES-256-CBC.

### 2. Proteção Anti-CSRF com State Criptográfico
O estado de autorização é empacotado em JSON codificado em Base64 assinado via HMAC-SHA256 contendo `TenantId`, `WorkspaceId`, `Platform`, `RedirectUri` e carimbo UTC de criação com janela de expiração (TTL) de 15 minutos. Alterações manuais no payload invalidam a assinatura e rejeitam o callback.

### 3. Criptografia AES-256 no Token Vault
- A entidade `OAuthTokenVault` armazena `EncryptedAccessToken` e `EncryptedRefreshToken`.
- O serviço `OAuthEncryptionService` encapsula o algoritmo AES-256-CBC com vetor de inicialização criptograficamente aleatório (16 bytes) exclusivo por registro.
- A chave simétrica é isolada das camadas externas e provida via injeção de dependência.

### 4. Suporte Específico às 4 Redes
- **Meta Ads:** Troca automática do *short-lived token* por *long-lived token* de 60 dias (`grant_type=fb_exchange_token`) e renovação preventiva.
- **Google Ads:** Fluxo com `access_type=offline` e `prompt=consent` para garantir emissão do `refresh_token`.
- **Bing Ads:** Microsoft Identity Platform v2.0 com escopos `msads.manage` e renovação via `refresh_token`.
- **TikTok Ads:** TikTok Marketing API v1.3 com retorno de `advertiser_ids` vinculados.

## Consequências

### Positivas
- Respeito integral às regras arquiteturais 1, 3, 5, 8 e 9 do `AGENTS.md`.
- Segredos de clientes armazenados com máxima segurança criptográfica no banco isolado de cada tenant.
- Testabilidade desacoplada com handlers HTTP simulados (`TestHttpMessageHandler`), garantindo 100% de cobertura nos testes unitários e zero dependência de redes externas em ambientes de teste.
- Roteamento e extensibilidade simples para adição de novas redes futuras (ex.: Pinterest Ads, LinkedIn Ads) através da interface `IOAuthAdapter`.

### Negativas / Mitigações
- Necessidade de configuração prévia das chaves de aplicação (`AppId`, `AppSecret`) por rede via variáveis de ambiente (`.env`).
- Variação nos formatos de resposta de erro entre as 4 plataformas: mitigada pelo encapsulamento de erros semânticos específicos nos adaptadores retornando `Result.Failure`.
