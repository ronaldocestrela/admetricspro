# ADR 0034: Orquestração de Operações em Massa Multiplataforma com Tolerância a Falhas Parciais

## Status
Aceito

## Data
2026-09-16

## Contexto
Na gestão de tráfego pago para múltiplas contas e clientes, gestores e analistas precisam frequentemente executar mutações simultâneas em dezenas de campanhas (ativar, pausar ou reajustar orçamentos diários percentual ou fixamente) distribuídas entre redes heterogêneas (Meta Ads, Google Ads, TikTok Ads e Bing Ads).

No modelo transacional padrão "tudo ou nada" (all-or-nothing), se uma única campanha de um lote de 30 falhar (ex.: campanha inexistente, restrição de status ou inconsistência temporária de rede), todas as outras 29 operações seriam revertidas. Em operações operacionais em larga escala, essa reversão completa degrada severamente a experiência do usuário e a produtividade da agência.

Além disso, a arquitetura do AdMetricsPro estabelece princípios inegociáveis:
1. Padrão `Result<T>` estrito (proibição de exceptions para fluxo de negócio).
2. Isolamento de tenant por banco de dados (`TenantDbContext`).
3. Frontend Blazor Server desacoplado de qualquer acesso direto ao banco ou infraestrutura, consumindo exclusivamente a Web API via `HttpClient` fortemente tipado.

## Decisão
Decidimos implementar o padrão de **Tolerância a Falhas Parciais (Partial-Success Pattern)** na camada de aplicação do módulo `Integrations`:

1. **Comando em Lote (`BulkCampaignOperationCommand`):**  
   Recebe o `WorkspaceId` e a lista de operações (`BulkCampaignOperationItem`), aceitando lote de até 100 itens.
2. **Orquestrador de Mutação (`BulkCampaignOperationCommandHandler`):**  
   Itera sobre as operações, validando o pertencimento de cada campanha ao workspace do inquilino. Executa as mutações de domínio (`Activate()`, `Pause()`, `UpdateDailyBudget()`) isoladamente por item.  
   - Itens válidos são coletados em `SucceededItems` e persistidos atomicamente via `IIntegrationsUnitOfWork.CommitAsync()`.
   - Itens que violam invariantes ou regras de negócio são coletados em `FailedItems` com código semântico (`ErrorCode`) e mensagem descritiva (`ErrorMessage`), sem interromper os itens válidos.
   - O envelope retornado é `Result<BulkCampaignOperationResultDto>.Success(...)`, contendo `TotalRequested`, `TotalSucceeded`, `TotalFailed`, `SucceededItems` e `FailedItems`.
3. **Endpoint RESTful Versionado:**  
   Exposto em `POST /api/v1/integrations/campaigns/bulk` com documentação OpenAPI + Scalar UI.
4. **Matriz de Edição Rápida com Prévia de Impacto:**  
   Componente Blazor `BulkCampaignEditorMatrix.razor` que permite seleção de campanhas de diferentes plataformas, calcula o impacto orçamentário agregado antes da confirmação (modal de confirmação com visualização de variação diária e projeção mensal) e renderiza feedback detalhado das mutações e falhas parciais.

## Consequências
### Positivas
- **Alta Resiliência e Eficiência:** Campanhas válidas têm suas ações aplicadas mesmo que parte do lote falhe, com transparência total para o usuário.
- **Prevenção de Erros Humanos:** A pré-visualização de impacto orçamentário calcula e exibe exatamente o delta monetário (R$) e percentual (%) antes de qualquer confirmação.
- **Conformidade Arquitetural Total:** Respeita o isolamento por tenant, padrão `Result<T>`, ausência de vazamento de persistência para o Blazor e testes unitários/bUnit com cobertura de ponta a ponta.

### Negativas / Mitigações
- **Complexidade no Cliente:** O frontend precisa renderizar relatórios que contemplam cenários de sucesso parcial. Mitigado através do componente `BulkCampaignEditorMatrix` com relatório visual embutido (`execution-report-card`).
