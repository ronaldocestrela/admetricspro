# 0037. Gerador de Relatórios Automatizados em White-Label e Disparador Multi-Canal

## Status
Aceito

## Contexto
Agências parceiras de gestão de tráfego pago investem dezenas de horas mensais compilando dados de múltiplas plataformas (Meta, Google, Bing, TikTok) em planilhas ou ferramentas de terceiros para enviar relatórios de prestação de contas aos clientes.

Os principais desafios identificados eram:
1. **Identidade White-Label Estrita (Zero-Leakage):** As agências exigem que nenhum artefato ou tela compartilhado com seus clientes finais mencione a plataforma AdMetricsPro. O documento ou página deve estampar exclusivamente a marca, logotipo, cores, rodapé e domínio personalizado da agência parceira.
2. **Saídas Híbridas Concorrentes:** Clientes distintos possuem preferências diversas: executivos seniores e diretorias financeiras demandam documentos em **PDF vetorial de alta resolução** para arquivamento e impressão, enquanto gestores e analistas preferem **links web interativos responsivos** que permitam visualização dinâmica em celulares ou desktops sem necessidade de login.
3. **Disparos Periódicos Multi-Canal:** Necessidade de agendar disparos automáticos com periodicidade diária, semanal ou mensal, entregando tanto por **E-mail corporativo** quanto por **WhatsApp**, garantindo que o cliente receba o resumo e o link onde ele estiver ativo.

## Decisão
Decidimos implementar o subsistema de **Gerador de Relatórios Automatizados em White-Label** (Subfase 5.4) sob a seguinte arquitetura:

1. **Domínio e Persistência Isolada (`BuildingBlocks.Domain.Reports` & `TenantDbContext`):**
   - Entidade `ReportSchedule`: regras de recorrência com cálculo automático de próxima execução (`Daily`, `Weekly`, `Monthly`), intervalos de métricas padrão e lista de destinatários com validação de formato (`ReportRecipient`).
   - Entidade `GeneratedReport`: imutabilidade dos dados históricos contendo o snapshot das métricas em JSON (`RenderModelJson`), snapshot de identidade visual (`ReportBrandingSnapshot`) e token criptográfico seguro de compartilhamento de 24 bytes hexadecimais com data de validade.
   - Entidade `ReportDispatchLog`: rastreabilidade completa e auditoria de cada disparo efetuado.
   - Persistência e isolamento no banco do tenant (`TenantDbContext`), sem compartilhamento de dados entre clientes.

2. **Geração de PDF Vetorial Nativamente em .NET 10 (`WhiteLabelReportPdfGenerator`):**
   - Desenvolvemos um gerador vetorial puro em C# (.NET 10) sem dependências externas complexas ou vulneráveis (como Chromium headless ou wkhtmltopdf).
   - O gerador compõe capa institucional com cores da agência, cartões de KPIs executivos, tabelas de distribuição por canal, destaques de criativos e rodapé estritamente white-label.

3. **Orquestrador de Disparo Multi-Canal (`ReportDispatchService`):**
   - E-mail: monta mensagem com template corporativo limpo e anexa o arquivo PDF gerado.
   - WhatsApp: integra com o gateway HTTP corporativo (`IReportWhatsAppNotifier`), enviando resumo dos KPIs principais e link direto de acesso com o token seguro.

4. **Web API e Rota Pública Anônima:**
   - `/api/v1/workspaces/{workspaceId}/reports/...`: endpoints autenticados para gestão de regras, compilação sob demanda e auditoria de entregas.
   - `/api/v1/public/reports/{token}`: endpoint seguro anônimo (`[AllowAnonymous]`) para resolução dos dados do relatório compartilhado.

5. **Frontend Blazor Server & Isolamento Arquitetural:**
   - Tela de gestão `ReportsManagementPage.razor`: listagem de agendamentos, histórico de envios, modais de agendamento e geração avulsa.
   - Página pública `/reports/shared/{token}` (`/r/{token}`) utilizando `EmptyLayout.razor` e tokens CSS injetados dinamicamente com base nas cores da agência.
   - Zero acesso direto a banco de dados no frontend: consumo exclusivo via `IReportClientService` e `IPublicReportClientService` via `HttpClient`.

## Consequências

### Positivas
- **Agilidade Operacional para Agências:** Elimina centenas de horas manuais de compilação de relatórios, aumentando a retenção das agências no plano SaaS.
- **Zero-Leakage de Marca:** Garante fidelidade 100% à identidade da agência, permitindo cobrança premium pelo serviço de tráfego pago.
- **Segurança de Acesso:** Tokens com criptografia segura e controle estrito de expiração, impedindo visualização perpétua não autorizada de dados analíticos.
- **Alta Eficiência e Portabilidade:** Geração de PDF vetorial puro sem consumo excessivo de memória ou dependências de navegadores headless em servidores Linux.

### Negativas / Mitigações
- **Volume de Armazenamento de PDFs:** A persistência binária de dezenas de relatórios por tenant pode onerar o banco ou storage.
  - *Mitigação:* O relatório armazena seu modelo de dados consolidado em JSON (`RenderModelJson`). O PDF pode ser sintetizado sob demanda e sob cache temporário, mantendo o consumo de disco mínimo.
