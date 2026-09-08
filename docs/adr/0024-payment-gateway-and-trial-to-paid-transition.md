# ADR 0024: Gateway de Pagamentos e Transição de Trial para Assinatura Paga

## Status
Aceito

## Data
2026-09-07

## Contexto
O modelo de negócio da AdMetricsPro baseia-se em autoatendimento (*self-service* e *product-led growth*), no qual agências ingressam através de um período de degustação gratuita de 14 dias (*Trial*). Para garantir a conversão sustentável em clientes pagantes:
1. O inquilino deve poder contratar um plano de assinatura comercial (`Starter`, `Pro`, `Enterprise`) a qualquer momento, escolhendo entre faturamento mensal ou anual com desconto agressivo (20% OFF).
2. O sistema deve suportar métodos de pagamento brasileiros essenciais: **Cartão de Crédito** (com aprovação síncrona/imediata) e **Pix** (com geração dinâmica de QR Code do Banco Central, payload Copia e Cola e conciliação por webhooks ou verificação de status).
3. Ao liquidar a transação financeira:
   - O status do `Tenant` deve migrar de `TenantStatus.Trial` para `TenantStatus.Active`.
   - O nível (`Tier`) e ciclo (`BillingCycle`) devem ser gravados no registro do inquilino.
   - O prazo de expiração da assinatura (`SubscriptionExpiresAtUtc`) deve ser estendido de acordo com a periodicidade (+30 dias para mensal ou +365 dias para anual).
   - A régua de inadimplência/dunning do inquilino deve ser regularizada (`RegularizePayment()`).
   - Um evento de domínio `TenantSubscriptionActivatedDomainEvent` deve ser publicado pelo agregado para disparar efeitos colaterais de forma desacoplada (disparo do e-mail de confirmação e auditoria em `TenantNotificationLog`).
4. Todo o fluxo de checkout e faturamento no frontend (Blazor Server) deve respeitar a Regra 9 do `AGENTS.md` (zero acesso direto a banco, consumo estrito via Web API com `Result<T>`).

## Decisão

### 1. Modelagem de Domínio e Agregado de Pagamentos (`Master.Domain/Billing`)
- **Entidades e Enums:**
  - `PaymentMethod`: `CreditCard = 1`, `Pix = 2`.
  - `PaymentTransactionStatus`: `Pending = 1`, `Paid = 2`, `Failed = 3`, `Cancelled = 4`, `Refunded = 5`.
  - `TenantPaymentTransaction`: Entidade de domínio que registra todas as tentativas de transação, valor, método, detalhes do cartão anonimizados (últimos 4 dígitos e bandeira), payloads do Pix (QR Code em base64 e chave Copia e Cola), ID do gateway externo e data de liquidação.
- **Transição de Estado no Agregado `Tenant`:**
  - Método de domínio `ActivatePaidSubscription(SubscriptionTier tier, string billingCycle, DateTime referenceUtc, decimal amount)`.
  - Atualiza o status para `TenantStatus.Active`, redefine a data de expiração, limpa pendências de inadimplência e emite o evento `TenantSubscriptionActivatedDomainEvent`.

### 2. Abstração de Gateway de Pagamento e Implementações (`Master.Infrastructure/Payments`)
- Contrato `IPaymentGatewayService` no Kernel da aplicação com métodos:
  - `ProcessCreditCardPaymentAsync(...)`
  - `GeneratePixPaymentAsync(...)`
  - `GetPaymentStatusAsync(...)`
- Implementações:
  - `AsaasPaymentGateway`: Integração REST oficial com a API do Asaas (suportando `/payments`, `/payments/{id}/pixQrCode`, etc.) com autenticação via `access_token` configurável via `AsaasOptions`.
  - `InMemoryPaymentGateway`: Mock in-memory resiliente para testes automatizados, CI/CD e desenvolvimento local sem necessidade de credenciais externas.
- Resolução via Injeção de Dependência selecionada de acordo com as variáveis de ambiente (`PAYMENT_GATEWAY__PROVIDER=Asaas` ou `InMemory`).

### 3. Persistência no Catálogo Mestre (`MasterDbContext`)
- Mapeamento EF Core `TenantPaymentTransactionEntityTypeConfiguration` gerando a tabela `TenantPaymentTransactions` no `MasterDb`.
- Criação de índices para consultas eficientes por inquilino e ID externo (`IX_TenantPaymentTransactions_TenantId_CreatedAtUtc`, `IX_TenantPaymentTransactions_GatewayTransactionId`).
- Migração versionada `20260908013735_Add_TenantPaymentTransactions`.

### 4. Fluxo CQRS e Webhooks na Web API (`BillingController`)
- `GET /api/v1/billing/checkout/preview`: Pré-visualização de valores líquidos, descontos percentuais e data de renovação.
- `POST /api/v1/billing/checkout`: Processamento de pagamento transparente com ativação imediata (cartão aprovado) ou geração de Pix.
- `GET /api/v1/billing/checkout/{transactionId}/status`: Consulta pontual de liquidação com ativação automática em caso de quitação.
- `POST /api/v1/billing/webhooks/{provider}`: Processamento de notificações assíncronas do gateway (ex: eventos `PAYMENT_RECEIVED` ou `PAYMENT_CONFIRMED`).

### 5. Frontend Blazor Server (`/settings/billing`)
- Cliente HTTP tipado `IBillingClientService` registrado no container com bypass de certificados em desenvolvimento.
- Página rica `TenantBillingPage.razor` com seletor de planos (Starter, Pro, Enterprise), alternador de periodicidade com badge de economia (20% OFF), formulário de cartão e exibição interativa de Pix com botão Copia e Cola.
- Banner de degustação `TenantTrialBanner` integrado ao `TenantState`, desaparecendo automaticamente após a confirmação da transação.

## Consequências

- **Positivas:**
  - Ciclo de receita previsível: transição suave de trial para clientes pagantes sem intervenção manual do suporte.
  - Segurança e conformidade PCI: dados sensíveis de cartão não são persistidos em banco relacional, apenas metadados seguros (últimos 4 dígitos e bandeira).
  - Experiência otimizada de conversão: suporte a Pix com liquidação em segundos e cartão com ativação instantânea.
  - Testabilidade 100%: testes de unidade, integração e aceitação validados sem depender de conexões com ambientes sandbox instáveis.
- **Mitigações:**
  - Falhas transitórias no recebimento de webhooks são mitigadas pelo endpoint de polling de status no frontend (`/status`) e pela conciliação automática do gateway.
