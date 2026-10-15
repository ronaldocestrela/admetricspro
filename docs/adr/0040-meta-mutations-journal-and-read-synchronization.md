# ADR 0040: Diário idempotente de escritas Meta e sincronização periódica de leitura

## Status
Implementado localmente; aplicação em SQL Server e validação real na Meta pendentes.

## Contexto
Repetir criação após timeout pode duplicar campanha, conjunto ou anúncio. Atualizar apenas o banco local não comprova uma mudança na Meta. A sincronização precisa preservar o banco do tenant e o workspace ao rodar fora do pipeline HTTP.

## Decisão

- Registrar uma operação `MetaApiOperation` no banco do tenant antes do POST externo. A chave de idempotência é única, contextualizada por workspace/conta e vinculada a um hash do pedido.
- Consolidar a reserva pelo UnitOfWork antes da rede. Armazenar somente metadados de auditoria e resultado sanitizado; não persistir tokens ou payload completo nesse diário.
- Não repetir POST automaticamente. Repetições de sucesso retornam o resultado gravado. Pedidos com resultado pendente/incerto exigem reconciliação; a ausência de resposta HTTP não implica rollback na Meta.
- Fazer consultas de propriedade da conta e dos pais/ativos antes de editar ou associar objetos. Criações publicitárias são pausadas. Ativação exige pedido explícito com confirmação.
- Encaminhar os comandos existentes de pausa, ativação e orçamento para essa mesma implementação antes de atualizar o estado local de contas Meta reais. Preservar o modo demonstrativo identificado. Não simular atomicidade de realocação em duas campanhas reais.
- Reutilizar BackgroundService, MediatR, repositórios e resolução de tenant existentes para leitura periódica. Habilitação é explícita por configuração e desabilitada por padrão. Não agendar mutações publicitárias.
- Criar escopo novo para cada conta e atribuir tenant antes da resolução dos repositórios. Reprocessar janela recente no fuso da conta e registrar execução sem credenciais.
- Não adicionar fila, SDK ou biblioteca de resiliência. O transporte HTTP Meta controla paginação por cursor e retries limitados somente para leituras.
- O índice de métricas SQL Server deve incluir dimensões nulas. O filtro convencional de índices únicos do EF excluía métricas diárias em nível de campanha, deixando-as sem proteção contra duplicação.

## Consequências

- Existe auditoria consultável de intenção e resultado, inclusive após perda da resposta ao navegador.
- Uma falha entre confirmação remota e persistência local deixa estado pendente; não há transação distribuída com a Meta. Reconciliar antes de criar outra intenção.
- A sincronização sequencial evita sobreposição de ciclos na instância; múltiplas réplicas ainda exigem um único worker habilitado ou futura lease distribuída.
- Consultas paginadas grandes continuam síncronas; jobs assíncronos de Insights são evolução posterior.
- A escrita exige usuário autorizado no contexto. Automação real sem usuário não recebe privilégio implícito para gastar; uma identidade de execução e política específicas precisam ser definidas antes de habilitar esse cenário.
