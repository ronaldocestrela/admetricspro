# Especificação de Funcionalidades — Gestão de Tenants, Equipes e White-Label

Este documento detalha a arquitetura funcional e as regras de negócio para o módulo de **Gerenciamento de Tenants (Multi-Tenancy)**, **White-Label**, **Gestão de Times (Squads)** e **Controle de Acessos Granular** para o SaaS de Gerenciamento Unificado de Anúncios (Meta Ads, Google Ads, Bing Ads e TikTok Ads).

---

## 1. Arquitetura Hierárquica Multi-Tenant

O sistema implementa isolamento lógico estrito entre contratantes, permitindo uma estrutura organizacional escalável em quatro níveis:

```text
[ Tenant / Organização ] (Dono da assinatura, ex: Agência Alfa)
    │
    ├── [ Times / Squads ] (Agrupamento de colaboradores internos)
    │       │
    │       └── Membros (Gestores de tráfego, analistas, operadores)
    │
    └── [ Workspaces / Clientes ] (Clientes atendidos ou unidades de negócio)
            │
            ├── Contas de Anúncio Conectadas (Meta, Google, Bing, TikTok)
            └── Acesso Concedido a Times Específicos e Clientes Finais

```

### 1.1 Entidades do Sistema

* **Tenant (Organização Principal):** Entidade jurídica assinante do SaaS. Detém o faturamento, as configurações globais de personalização e a gestão de planos.
* **Workspaces / Clientes:** Ambientes isolados dentro do Tenant que agrupam as contas de mídia de um cliente específico ou marca própria.
* **Contas de Mídia Conectadas:** Integrações OAuth2 com Meta Ads, Google Ads, Microsoft Advertising (Bing) e TikTok Ads vinculadas diretamente ao Workspace do cliente.
* **Times (Squads):** Grupos de trabalho operacionais da organização (ex.: *Squad E-commerce*, *Squad Infoprodutos*, *Squad B2B*).

---

## 2. Módulo White-Label & Personalização de Marca

Permite ao Tenant comercializar ou apresentar a plataforma aos seus clientes finais com identidade visual própria, gerando autoridade de marca.

### 2.1 Identidade Visual nos Relatórios e Painéis

* **Logomarca Customizada:**
* Upload de logotipo em alta resolução para temas claros e escuros.
* Ícone compacto (Favicon) para exibição em portais web.


* **Paleta de Cores Institucional:**
* Definição de cor primária, secundária e de destaque aplicadas aos cabeçalhos, botões, gráficos e PDFs.


* **Dados Institucionais no Rodapé:**
* Razão social, CNPJ, site, e-mail de suporte e telefone de contato nos relatórios gerados.


* **Remoção de Powered-By:**
* Opção de ocultar completamente qualquer menção à marca do SaaS no rodapé de relatórios e telas compartilhadas (recurso para planos intermediários e avançados).



### 2.2 Domínio Personalizado (Custom Domain / CNAME)

* **Subdomínio Próprio:** Possibilidade de o Tenant apontar um subdomínio próprio (ex.: `analytics.suaagencia.com.br` ou `relatorios.suaagencia.com.br`).
* **Provisionamento Automático de SSL:** Geração e renovação automática de certificados TLS/HTTPS via Let's Encrypt para os domínios mapeados.
* **Página de Login Personalizada:** Tela de autenticação exclusiva com a marca e cores da agência quando acessada via subdomínio próprio.

### 2.3 Modelos de Relatórios Salvos (Templates)

* Criação de templates padronizados pela agência contendo métricas preferidas, seções executivas e textos explicativos reutilizáveis entre múltiplos clientes.

---

## 3. Gestão de Times (Squads) e Alocação por Clientes

Permite que o Tenant organize seus colaboradores em squads temáticos ou regionais, restringindo a visibilidade estritamente aos clientes autorizados.

### 3.1 Criação e Configuração de Times

* **Estrutura de Squads:** O administrador cria grupos de trabalho (ex.: *Time Varejo*, *Time Performance Norte*).
* **Alocação de Clientes ao Time:** Associação em massa de quais Workspaces/Clientes pertencem a qual squad.
* **Isolamento entre Times:**
* Um colaborador alocado exclusivamente no *Time Varejo* não tem visibilidade nem acesso a relatórios, campanhas ou métricas dos clientes atribuídos ao *Time B2B*.


* **Colaboradores Multitime:** Flexibilidade para que líderes técnicos, gestores seniores ou especialistas de canal (ex.: especialista exclusivo em TikTok Ads) participem de múltiplos squads simultaneamente.

---

## 4. Matriz de Perfis e Permissões (RBAC Granular)

O controle de acesso atua em duas camadas: **o que o usuário pode fazer** (papel funcional) e **onde ele pode atuar** (escopo de clientes/squads).

| Papel (Role) | Escopo de Visualização | Criação / Edição de Campanhas | Ajuste de Orçamento e Regras | Faturamento e Assinatura | Gestão de Usuários / Times |
| --- | --- | --- | --- | --- | --- |
| **Owner / Dono** | Todos os Workspaces | Sim | Sim | Sim | Sim |
| **Administrador** | Todos os Workspaces | Sim | Sim | Não | Sim |
| **Líder de Squad** | Apenas clientes do Squad | Sim | Sim | Não | Gerencia membros do Squad |
| **Gestor de Mídia** | Apenas clientes do Squad | Sim | Sim (até teto definido) | Não | Não |
| **Analista de Tráfego** | Apenas clientes do Squad | Sim | Não | Não | Não |
| **Visualizador Interno** | Apenas clientes do Squad | Não (Somente leitura) | Não | Não | Não |
| **Cliente Convidado** | Apenas seu próprio Workspace | Não (Somente leitura) | Não | Não | Não |

### 4.1 Acesso Exclusivo para Cliente Final (*Client Guest Portal*)

* **Visão Blindada:** Acesso restrito somente leitura às métricas do próprio negócio.
* **Ocultação de Dados Sensíveis:** O cliente não visualiza:
* Histórico ou existência de outros clientes da agência.
* Custos de ferramentas internas, comissões ou markup de agência.
* Regras de automação confidenciais ou notas internas dos gestores de tráfego.



---

## 5. Auditoria, Governança e Segurança

### 5.1 Trilha de Auditoria (Audit Logs)

* Registro imutável de todas as ações operacionais e administrativas realizadas no Tenant:
* *Quem realizou:* E-mail e IP do usuário.
* *O que foi feito:* Ex.: Alteração de orçamento, criação de regra de automação, exclusão de anúncio, convite de novo usuário.
* *Onde:* Cliente e campanha de destino.
* *Quando:* Data e horário exatos.


* Filtro avançado para investigação rápida de incidentes (ex.: identificar quem subiu o orçamento de forma indevida).

### 5.2 Políticas de Segurança de Acesso

* **Autenticação de Dois Fatores (2FA/MFA):** Opção do administrador exigir 2FA obrigatório via TOTP (Google Authenticator, Authy) para todos os colaboradores do Tenant.
* **Revogação Imediata de Sessão:** Bloqueio instantâneo de colaboradores desligados com encerramento de todas as sessões ativas em navegadores e APIs.

---

## 6. Painel de Faturamento e Controle de Limites do Tenant

Área restrita aos administradores e proprietários do Tenant para acompanhar o uso dos recursos contratados.

### 6.1 Monitoramento de Uso (*Resource Quotas*)

* Barra de progresso com consumo em tempo real:
* Quantidade de Workspaces/Clientes cadastrados vs. Limite do plano.
* Assentos de usuários (*seats*) ativos vs. Contratados.
* Volume total de investimento em mídia gerenciada no ciclo atual (*Monthly Ad Spend Limit*).



### 6.2 Autoatendimento de Assinatura

* Upgrade ou downgrade imediato de planos com cálculo proporcional (*pro-rata*).
* Contratação de pacotes avulsos (Add-ons):
* Pacote adicional de 5 usuários.
* Pacote adicional de 10 clientes/workspaces.
* Adicional de domínio personalizado extra.


* Histórico financeiro com download de faturas e notas fiscais.


output_path = "especificacao_gerenciamento_tenants_saas.md"
with open(output_path, "w", encoding="utf-8") as f:
f.write(markdown_content)

print(f"File created: {output_path}")

```

Aqui está a especificação funcional completa em formato **Markdown (`.md`)** cobrindo todos os requisitos de arquitetura multi-tenant, personalização white-label, estrutura de times e controle de acesso granular:

```markdown
# Especificação de Funcionalidades — Gestão de Tenants, Equipes e White-Label

Este documento detalha a arquitetura funcional e as regras de negócio para o módulo de **Gerenciamento de Tenants (Multi-Tenancy)**, **White-Label**, **Gestão de Times (Squads)** e **Controle de Acessos Granular** para o SaaS de Gerenciamento Unificado de Anúncios (Meta Ads, Google Ads, Bing Ads e TikTok Ads).

---

## 1. Arquitetura Hierárquica Multi-Tenant

O sistema implementa isolamento lógico estrito entre contratantes, permitindo uma estrutura organizacional escalável em quatro níveis:

```text
[ Tenant / Organização ] (Dono da assinatura, ex: Agência Alfa)
    │
    ├── [ Times / Squads ] (Agrupamento de colaboradores internos)
    │       │
    │       └── Membros (Gestores de tráfego, analistas, operadores)
    │
    └── [ Workspaces / Clientes ] (Clientes atendidos ou unidades de negócio)
            │
            ├── Contas de Anúncio Conectadas (Meta, Google, Bing, TikTok)
            └── Acesso Concedido a Times Específicos e Clientes Finais

```

### 1.1 Entidades do Sistema

* **Tenant (Organização Principal):** Entidade jurídica assinante do SaaS. Detém o faturamento, as configurações globais de personalização e a gestão de planos.
* **Workspaces / Clientes:** Ambientes isolados dentro do Tenant que agrupam as contas de mídia de um cliente específico ou marca própria.
* **Contas de Mídia Conectadas:** Integrações OAuth2 com Meta Ads, Google Ads, Microsoft Advertising (Bing) e TikTok Ads vinculadas diretamente ao Workspace do cliente.
* **Times (Squads):** Grupos de trabalho operacionais da organização (ex.: *Squad E-commerce*, *Squad Infoprodutos*, *Squad B2B*).

---

## 2. Módulo White-Label & Personalização de Marca

Permite ao Tenant comercializar ou apresentar a plataforma aos seus clientes finais com identidade visual própria, gerando autoridade de marca.

### 2.1 Identidade Visual nos Relatórios e Painéis

* **Logomarca Customizada:**
* Upload de logotipo em alta resolução para temas claros e escuros.
* Ícone compacto (Favicon) para exibição em portais web.


* **Paleta de Cores Institucional:**
* Definição de cor primária, secundária e de destaque aplicadas aos cabeçalhos, botões, gráficos e PDFs.


* **Dados Institucionais no Rodapé:**
* Razão social, CNPJ, site, e-mail de suporte e telefone de contato nos relatórios gerados.


* **Remoção de Powered-By:**
* Opção de ocultar completamente qualquer menção à marca do SaaS no rodapé de relatórios e telas compartilhadas (recurso para planos intermediários e avançados).



### 2.2 Domínio Personalizado (Custom Domain / CNAME)

* **Subdomínio Próprio:** Possibilidade de o Tenant apontar um subdomínio próprio (ex.: `analytics.suaagencia.com.br` ou `relatorios.suaagencia.com.br`).
* **Provisionamento Automático de SSL:** Geração e renovação automática de certificados TLS/HTTPS via Let's Encrypt para os domínios mapeados.
* **Página de Login Personalizada:** Tela de autenticação exclusiva com a marca e cores da agência quando acessada via subdomínio próprio.

### 2.3 Modelos de Relatórios Salvos (Templates)

* Criação de templates padronizados pela agência contendo métricas preferidas, seções executivas e textos explicativos reutilizáveis entre múltiplos clientes.

---

## 3. Gestão de Times (Squads) e Alocação por Clientes

Permite que o Tenant organize seus colaboradores em squads temáticos ou regionais, restringindo a visibilidade estritamente aos clientes autorizados.

### 3.1 Criação e Configuração de Times

* **Estrutura de Squads:** O administrador cria grupos de trabalho (ex.: *Time Varejo*, *Time Performance Norte*).
* **Alocação de Clientes ao Time:** Associação em massa de quais Workspaces/Clientes pertencem a qual squad.
* **Isolamento entre Times:**
* Um colaborador alocado exclusivamente no *Time Varejo* não tem visibilidade nem acesso a relatórios, campanhas ou métricas dos clientes atribuídos ao *Time B2B*.


* **Colaboradores Multitime:** Flexibilidade para que líderes técnicos, gestores seniores ou especialistas de canal (ex.: especialista exclusivo em TikTok Ads) participem de múltiplos squads simultaneamente.

---

## 4. Matriz de Perfis e Permissões (RBAC Granular)

O controle de acesso atua em duas camadas: **o que o usuário pode fazer** (papel funcional) e **onde ele pode atuar** (escopo de clientes/squads).

| Papel (Role) | Escopo de Visualização | Criação / Edição de Campanhas | Ajuste de Orçamento e Regras | Faturamento e Assinatura | Gestão de Usuários / Times |
| --- | --- | --- | --- | --- | --- |
| **Owner / Dono** | Todos os Workspaces | Sim | Sim | Sim | Sim |
| **Administrador** | Todos os Workspaces | Sim | Sim | Não | Sim |
| **Líder de Squad** | Apenas clientes do Squad | Sim | Sim | Não | Gerencia membros do Squad |
| **Gestor de Mídia** | Apenas clientes do Squad | Sim | Sim (até teto definido) | Não | Não |
| **Analista de Tráfego** | Apenas clientes do Squad | Sim | Não | Não | Não |
| **Visualizador Interno** | Apenas clientes do Squad | Não (Somente leitura) | Não | Não | Não |
| **Cliente Convidado** | Apenas seu próprio Workspace | Não (Somente leitura) | Não | Não | Não |

### 4.1 Acesso Exclusivo para Cliente Final (*Client Guest Portal*)

* **Visão Blindada:** Acesso restrito somente leitura às métricas do próprio negócio.
* **Ocultação de Dados Sensíveis:** O cliente não visualiza:
* Histórico ou existência de outros clientes da agência.
* Custos de ferramentas internas, comissões ou markup de agência.
* Regras de automação confidenciais ou notas internas dos gestores de tráfego.



---

## 5. Auditoria, Governança e Segurança

### 5.1 Trilha de Auditoria (Audit Logs)

* Registro imutável de todas as ações operacionais e administrativas realizadas no Tenant:
* *Quem realizou:* E-mail e IP do usuário.
* *O que foi feito:* Ex.: Alteração de orçamento, criação de regra de automação, exclusão de anúncio, convite de novo usuário.
* *Onde:* Cliente e campanha de destino.
* *Quando:* Data e horário exatos.


* Filtro avançado para investigação rápida de incidentes (ex.: identificar quem subiu o orçamento de forma indevida).

### 5.2 Políticas de Segurança de Acesso

* **Autenticação de Dois Fatores (2FA/MFA):** Opção do administrador exigir 2FA obrigatório via TOTP (Google Authenticator, Authy) para todos os colaboradores do Tenant.
* **Revogação Imediata de Sessão:** Bloqueio instantâneo de colaboradores desligados com encerramento de todas as sessões ativas em navegadores e APIs.

---

## 6. Painel de Faturamento e Controle de Limites do Tenant

Área restrita aos administradores e proprietários do Tenant para acompanhar o uso dos recursos contratados.

### 6.1 Monitoramento de Uso (*Resource Quotas*)

* Barra de progresso com consumo em tempo real:
* Quantidade de Workspaces/Clientes cadastrados vs. Limite do plano.
* Assentos de usuários (*seats*) ativos vs. Contratados.
* Volume total de investimento em mídia gerenciada no ciclo atual (*Monthly Ad Spend Limit*).



### 6.2 Autoatendimento de Assinatura

* Upgrade ou downgrade imediato de planos com cálculo proporcional (*pro-rata*).
* Contratação de pacotes avulsos (Add-ons):
* Pacote adicional de 5 usuários.
* Pacote adicional de 10 clientes/workspaces.
* Adicional de domínio personalizado extra.


* Histórico financeiro com download de faturas e recibos.

```