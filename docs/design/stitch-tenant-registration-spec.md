# Especificação de Design de UI/UX para Google Stitch
## Tela de Cadastro e Provisionamento de Novo Tenant (Onboarding Wizard)

> **Destinatário:** Google Stitch / LLM de Design de Interface  
> **Produto:** **AdMetricsPro** — SaaS B2B de Gestão Unificada de Tráfego Pago (Meta Ads, Google Ads, TikTok Ads, Bing Ads)  
> **Paradigma Arquitetural:** Monólito Modular em .NET 10 & Blazor Server com isolamento físico **Database-per-Tenant** (SQL Server dedicado por inquilino).  
> **Tema Visual:** Dark Mode Premium, Rich Aesthetics, superfícies glassmórficas, iluminação radial suave, tipografia moderna e visual high-tech de engenharia de alta performance.  
> **Stitch Screen Gerada:** Projeto ID `12765985270394356342` | Screen ID `3dbfaa4e77c64cfdab9dd6b167479b8e` | Arquivo HTML: `docs/design/stitch-tenant-onboarding.html`

---

## 1. Prompt Mestre para o Google Stitch

Copie e cole o prompt abaixo diretamente no **Google Stitch**:

```text
Crie o design de alta fidelidade para a tela de "Cadastro e Provisionamento de Novo Inquilino (Tenant Onboarding)" do AdMetricsPro, uma plataforma SaaS corporativa de gestão unificada de tráfego pago (Meta, Google, TikTok e Bing Ads).

Estilo Visual:
- Dark Mode moderno e futurista com superfícies em tons profundos (#0a0e18, #0f131d, #171b26 e #1c1f2a).
- Acentos vibrantes: Violeta Elétrico (#4f46e5 / #c3c0ff), Verde Neon (#4edea3) para validações de sucesso e Ciano Tecnológico (#4cd7f6).
- Efeitos: Glassmorphism refinado (backdrop-filter: blur(20px)), bordas sutis translúcidas (1px solid rgba(255, 255, 255, 0.08)) e radial glow nos cantos superiores.
- Tipografia: 'Plus Jakarta Sans' para títulos e textos de UI, e 'JetBrains Mono' para dados técnicos, URLs, subdomínios e console de log.

Layout & Composição (Split-Screen 60/40 no Desktop 1440px):
1. Coluna da Esquerda (60% da largura):
   - Barra de Navegação Superior Minimalista com Logotipo vetorial do AdMetricsPro e botão "Já tenho conta / Login".
   - Stepper Horizontal de 4 etapas:
     1. Empresa (Ativo/Completo)
     2. Subdomínio & Identidade
     3. Plano & Recursos
     4. Administrador
   - Formulário do Passo Ativo com inputs customizados, ícones no prefixo, suporte a validações inline em tempo real e botões de navegação "Voltar" e "Avançar".

2. Coluna da Direita (40% da largura - Painel Fixo "Live Tenant Cockpit Preview"):
   - Um card glassmórfico interativo que reage dinamicamente aos dados preenchidos na esquerda:
     - Preview da URL de acesso: "https://{subdomain}.admetricspro.com.br" com indicador verde de "Subdomínio Disponível".
     - Badge do Plano Selecionado (ex: "PRO - R$ 497/mês" com tag "14 Dias Grátis").
     - Simulador de Branding White-Label: logo da empresa do cliente e paleta de cores customizada aplicadas em um mini-mockup de dashboard.
     - Selos de Confiança e Compliance no rodapé do preview: "Instância SQL Server Dedicada", "Criptografia AES-256 em Repouso", "Isolamento Físico de Dados" e "LGPD / GDPR Compliant".

3. Modal / Estado de "Provisionamento em Tempo Real" (Overlay):
   - Exibido ao submeter o formulário: animação circular de progresso (0% a 100%) e um console de telemetria estilizado com log passo a passo:
     - [1/5] Validando invariantes de domínio e unicidade de CNPJ... OK
     - [2/5] Criando banco de dados físico 'Tenant_{subdomain}' no SQL Server... OK
     - [3/5] Aplicando migrações estruturais do TenantDbContext (EF Core 10)... OK
     - [4/5] Encriptando credenciais com AES-256 e gravando no MasterDb... OK
     - [5/5] Provisionando credenciais do Super Usuário... Concluído!
   - Card de celebração com botão primário: "Acessar Meu Cockpit no AdMetricsPro ->".
```

---

## 2. Especificação Detalhada dos Componentes e Etapas

### Etapa 1: Dados Fiscais e Corporativos da Empresa
* **Objetivo:** Coletar a identificação jurídica e o perfil de uso para dimensionamento da conta.
* **Campos Obrigatórios:**
  * **Razão Social / Nome da Empresa:** Input de texto com ícone de prédio corporativo. Ex.: `Agência Vanguarda Digital Ltda`.
  * **CNPJ da Empresa:** Input com máscara numérica dinâmica `00.000.000/0000-00` e validação imediata de 14 dígitos.
  * **Segmento de Atuação (Select / Radio Cards):**
    * `Agência de Tráfego / Performance` (ícone de foguete/growth).
    * `E-commerce / D2C` (ícone de carrinho de compras).
    * `Infoprodutos / Lançamentos` (ícone de play/vídeo).
    * `Empresa B2B / Serviços` (ícone de maleta executiva).
  * **Faixa de Investimento Mensal em Mídia Paga (Dropdown):**
    * *Até R$ 20.000 / mês*
    * *R$ 20.000 a R$ 100.000 / mês*
    * *R$ 100.000 a R$ 500.000 / mês*
    * *Acima de R$ 500.000 / mês*

---

### Etapa 2: Subdomínio Exclusivo & Identidade White-Label
* **Objetivo:** Parametrizar a URL de roteamento do inquilino e a identidade visual da interface.
* **Campos e Elementos:**
  * **Subdomínio Dedicado (Input Integrado):**
    * Prefixo digitável: `[ sua-empresa ]` + Sufixo fixo `.admetricspro.com.br`.
    * Sanitização instantânea: conversão automática para minúsculas, remoção de espaços e acentos.
    * Indicador de Status com debounce de 300ms:
      * *Disponível:* Borda verde esmeralda (`#4edea3`), ícone de check e texto "Subdomínio reservado para sua instância".
      * *Indisponível:* Borda vermelha (`#f87171`), ícone de alerta e mensagem "Subdomínio já em uso por outro assinante".
  * **Domínio CNAME Personalizado (Opcional - Feature White-Label):**
    * Input para domínio próprio: `analytics.suaagencia.com.br`.
    * Tooltip explicativo: *"Permite que seus clientes acessem relatórios e dashboards diretamente pelo seu domínio corporativo."*
  * **Customização de Marca (Branding Inicial):**
    * **Upload de Logo:** Drag & Drop para arquivos PNG/SVG transparentes (versão Dark e versão Light).
    * **Cores do Tenant:** Seletor de Cor Primária (ex.: `#4F46E5`), Secundária (ex.: `#0F172A`) e Acento (ex.: `#38BDF8`) com preview imediato nas variáveis CSS do simulador.

---

### Etapa 3: Seleção do Plano e Ciclo de Faturamento
* **Objetivo:** Permitir ao usuário selecionar o tier adequado à sua operação, enfatizando o período de testes grátis.
* **Seletor de Ciclo:**
  * Toggle Switch: `Mensal` | `Anual (2 meses grátis / 20% OFF)`.
* **Cards Comparativos de Planos (Grid de 3 Colunas):**

| Recurso / Métrica | Starter | Pro (Recomendado / Glow) | Enterprise |
| :--- | :--- | :--- | :--- |
| **Público Alvo** | Gestores autônomos e consultorias | Agências em escala e e-commerces | Grandes agências e holdings |
| **Preço Estimado** | R$ 197 / mês | **R$ 497 / mês** (Destaque) | R$ 1.290 / mês |
| **Limite de Workspaces** | Até 3 Workspaces | **Até 15 Workspaces** | Workspaces Ilimitados |
| **Assentos de Usuários** | 2 Membros | **8 Membros** | Membros Ilimitados |
| **Teto de Ad Spend Gerenciado** | Até R$ 50k / mês | **Até R$ 300k / mês** | Sem teto de investimento |
| **Conectores Inclusos** | Meta Ads + Google Ads | **Meta + Google + TikTok + Bing** | Todas as redes + API aberta |
| **Recursos Avançados** | Relatórios padrão | **Regras de Pacing & Overspending + White-Label** | Copiloto IA + CNAME Próprio + Suporte VIP |
| **Período de Degustação** | 14 dias grátis | **14 dias grátis (Sem cartão)** | Demonstração assistida |

---

### Etapa 4: Credenciais do Administrador Responsável (Owner)
* **Objetivo:** Criar o primeiro usuário com permissões completas de Super Usuário/Owner no banco do novo tenant.
* **Campos:**
  * **Nome Completo:** Ex.: `Carlos Mendes`.
  * **E-mail Corporativo:** Ex.: `carlos@vanguardadigital.com.br` (com validação de formato e aviso se e-mail genérico @gmail/@hotmail for inserido).
  * **Cargo na Empresa:** Ex.: `Sócio / Head de Tráfego`.
  * **WhatsApp Comercial:** Ex.: `(11) 98765-4321` (para envio de notificações urgentes de overspending).
  * **Senha de Acesso:** Input de senha com botão de exibir/ocultar e régua de força com 4 níveis (Fraca, Média, Forte, Imbatível) avaliando tamanho, números, símbolos e maiúsculas.
  * **Confirmação de Senha:** Validação de igualdade com o campo anterior.
  * **Termos & Políticas:** Checkbox de aceite obrigatório com links modais para os Termos de Uso e Política de Tratamento de Dados (LGPD/GDPR).

---

### Etapa 5: Overlay / Tela de Provisionamento em Tempo Real
* **Objetivo:** Dar visibilidade e credibilidade à complexidade da infraestrutura física sendo gerada pelo `TenantProvisioningService` (.NET 10).
* **Componentes:**
  * **Spinner Circular de Engenharia:** Anel com gradiente giratório contendo a porcentagem centralizada (0% -> 25% -> 50% -> 75% -> 100%).
  * **Console de Telemetria (Terminal UI):**
    * Janela estilo macOS escura com botões vermelho/amarelo/verde no cabeçalho e título: `provisioning-agent://master.admetricspro.internal`.
    * Mensagens emitidas linha a linha simulando o pipeline do backend:
      ```bash
      [10:00:01] INITIATING: Criando entidade TenantId: 3fa85f64-5717-4562-b3fc-2c963f66afa6
      [10:00:02] VALIDATING: Unicidade de CNPJ 12.345.678/0001-95 e Subdomínio 'vanguarda'... APROVADO
      [10:00:03] INFRASTRUCTURE: Criando catálogo de banco SQL Server dedicado: [Tenant_vanguarda]...
      [10:00:04] MIGRATING: Aplicando 14 migrações do TenantOperationalDbContext (EF Core 10)... SUCESSO
      [10:00:05] SECURITY: Encriptando connection string do tenant com AES-256 no MasterDb... OK
      [10:00:06] IDENTITY: Gerando credenciais do usuário Owner carlos@vanguardadigital.com.br... CONCLUÍDO
      [10:00:07] HEALTH CHECK: Tenant provisionado e online com 100% de integridade!
      ```
  * **Estado Final (Sucesso):**
    * Transição suave para um card com ícone de checkmark verde pulsante.
    * Título: *"Seu ambiente dedicado foi provisionado com sucesso!"*
    * Informações de acesso: URL exclusiva `https://vanguarda.admetricspro.com.br`.
    * Botão primário CTA de alta visibilidade: **"Acessar Meu Cockpit do Workspace →"**.

---

## 3. Guia de Tokens e Estilos CSS para a LLM

```css
/* Paleta de Cores e Superfícies */
--bg-root: #0a0e18;
--bg-surface: #0f131d;
--bg-surface-card: #171b26;
--bg-surface-input: #1c1f2a;
--border-subtle: rgba(255, 255, 255, 0.08);
--border-focus: #4f46e5;

/* Cores de Destaque e Ação */
--color-primary: #4f46e5;
--color-primary-light: #c3c0ff;
--color-success: #4edea3;
--color-cyan: #4cd7f6;
--color-danger: #f87171;
--color-text-title: #f8fafc;
--color-text-body: #94a3b8;
--color-text-muted: #64748b;

/* Efeitos e Tipografia */
--glass-blur: blur(20px);
--card-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.45);
--font-sans: 'Plus Jakarta Sans', -apple-system, sans-serif;
--font-mono: 'JetBrains Mono', monospace;
```

---

## 4. Instruções de Responsividade e Casos de Borda

1. **Desktop (1440px / 1920px):**
   * Layout em 2 colunas: 60% formulário + stepper à esquerda; 40% preview interativo fixo à direita (`position: sticky; top: 2rem;`).
2. **Mobile / Tablet (390px / 768px):**
   * Layout em coluna única. O "Live Tenant Preview" é transformado em um banner colapsável no topo com resumo visual compacto do subdomínio e do plano.
3. **Casos de Borda e Erros Tratados Visualmente:**
   * CNPJ com formato incorreto ou já cadastrado no catálogo central: mensagem de alerta vermelha inline com link direto para o suporte ou recuperação de conta.
   * Tentativa de subdomínio reservado pelo sistema (ex: `admin`, `api`, `master`, `app`): aviso em amarelo informando que este identificador é de uso restrito da plataforma.
   * Conexão oscilante durante o provisionamento: retry visual automático com log indicando nova tentativa sem perder os dados preenchidos.
