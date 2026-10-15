# ADR 0039: Autorização Meta por workspace e vínculo explícito de contas

## Status
Aceito para implementação; validação externa depende de configuração e contas autorizadas.

## Contexto
Uma agência atende clientes distintos em workspaces do mesmo banco dedicado. O OAuth existente já é único por workspace/plataforma, mas não diferenciava o usuário Meta autorizador da conta de anúncios. O state podia ser reutilizado, o host não validava JWTs de tenant e não havia fluxo de seleção real das contas.

## Decisão

- Preservar o OAuth compartilhado e a autorização independente por workspace, conforme o modelo de produto solicitado. Não introduzir uma autorização global da agência.
- Validar JWTs existentes com assinatura, emissor, audiência e validade. Antes de resolver dados de integração, comparar tenant da identidade assinada e contexto HTTP; autorizar o workspace através de contrato MediatR atendido por Tenants.
- Assinar o state com HMAC, incluir usuário, tenant, workspace, callback e nonce de 256 bits. Persistir somente seu hash no banco do tenant e consumir por DELETE condicional atômico antes de trocar o código. Essa reserva de segurança tem commit imediato e deliberado: mesmo uma falha posterior exige iniciar um fluxo novo, evitando replay entre processos.
- Cifrar credenciais Meta com AES-256-GCM e chave externa, autenticando também tenant, workspace e ID da conexão. Credenciais Meta legadas exigem reconexão; preservar compatibilidade dos demais provedores. O ID de `/me` no cofre identifica o autorizador; contas de anúncios permanecem em ConnectedAdAccounts.
- Descobrir contas por `/me/adaccounts`, paginando por cursor em host fixo. Selecionar somente IDs confirmados pela autorização atual. Vincular conta e cofre por chave estrangeira composta que inclui WorkspaceId.
- Permitir várias contas por workspace e o mesmo ID externo em workspaces distintos. Unicidade por workspace, plataforma e ID externo. Toda sincronização exige vínculo ativo e cofre correspondente.
- Reconexão Meta invalida vínculos locais anteriores até nova seleção. Desconexão de workspace é local; não remover automaticamente `/me/permissions`, pois isso revoga a concessão do aplicativo e pode afetar outros workspaces.
- Tokens de usuário Meta não são renovados indefinidamente por refresh token. Credenciais válidas continuam operacionais até sua validade conhecida; vencimento/revogação exige reconexão.
- Desabilitar logging de URLs dos clientes Meta e do callback HTTP, evitando exposição de parâmetros sensíveis.
- Regularizar separadamente o schema operacional preexistente: o snapshot anterior não versionava tabelas de campanhas, métricas e auditoria, entre outras já presentes no modelo. A migration de reconciliação antecede a migration específica de states/vínculos; não executar automaticamente contra um banco já criado por EnsureCreated sem revisar seu histórico.

## Consequências

- Workspaces não compartilham credenciais nem herdam contas automaticamente de outra autorização.
- O backend pode sincronizar a mesma conta em dois workspaces legitimamente autorizados, com dados separados; relatórios consolidados precisam considerar eventual duplicidade de origem.
- Reiniciar uma autorização interrompida é obrigatório; states não sobrevivem ao consumo.
- Chaves de assinatura e criptografia precisam ser fornecidas e mantidas de forma persistente pelo operador. Instâncias do backend devem compartilhar as mesmas chaves.
- A operação externa deve ser testada em contas autorizadas. Aprovação da Meta e autorização da Sandbox não são substituídas por testes com HTTP simulado.
