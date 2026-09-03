# ADR 0009: Mensageria In-Memory Desacoplada (MediatR) e Pipeline de Validação com FluentValidation e Result Pattern

## Status
Aceito

## Contexto
O AdMetricsPro é projetado como um **Monólito Modular** em .NET 10. Para manter o isolamento arquitetural e o baixo acoplamento entre os módulos funcionais (ex.: `Master`, `Tenants`, `Integrations`, `Analytics`, `Automations`), é terminantemente proibido o compartilhamento direto de instâncias de `DbContext` ou a referência direta a repositórios de outros módulos. Toda interação inter-módulos para execução de comandos, consultas e propagação de eventos de domínio deve ocorrer através de contratos tipados e mediador em memória.

Adicionalmente, os princípios inegociáveis de engenharia de `AGENTS.md` exigem:
1. **Uso estrito do padrão `Result` / `Result<T>`:** É vetado o uso de exceções (`throw new Exception`, `throw new ValidationException`) para controle de fluxo de negócio ou validação de entrada de dados.
2. **Pipelines de validação automáticos:** Erros de validação de comandos e consultas devem ser interceptados antes de atingir os handlers de negócio e convertidos automaticamente em instâncias de falha (`Result.Failure(Error.Validation(...))`).
3. **Detalhamento estruturado de inconsistências:** Necessidade de mapear os campos/propriedades com problemas para consumo por clientes HTTP/OpenAPI e telas interativas do Blazor Server.

## Decisão
1. **Adoção do MediatR 14.x como Mediador In-Memory:**
   - Contratos CQRS base definidos em `BuildingBlocks.Application.Messaging`:
     - `ICommand` herda de `IRequest<Result>`.
     - `ICommand<TResponse>` herda de `IRequest<Result<TResponse>>`.
     - `ICommandHandler<TCommand>` herda de `IRequestHandler<TCommand, Result>`.
     - `ICommandHandler<TCommand, TResponse>` herda de `IRequestHandler<TCommand, Result<TResponse>>`.
     - `IQuery<TResponse>` herda de `IRequest<Result<TResponse>>`.
     - `IQueryHandler<TQuery, TResponse>` herda de `IRequestHandler<TQuery, Result<TResponse>>`.
   - Propagação de eventos de domínio desacoplados via envelope `DomainEventNotification<TDomainEvent>` implementando `INotification` e tratado por `IDomainEventHandler<TDomainEvent>`.

2. **Interceptador Genérico de Validação (`ValidationBehavior<TRequest, TResponse>`):**
   - Implementado como um `IPipelineBehavior<TRequest, TResponse>` aberto registrado no MediatR.
   - Executa todos os `IValidator<TRequest>` do `FluentValidation` registrados no container de injeção de dependências.
   - Quando violações de regra são detectadas:
     - O pipeline de execução é interrompido imediatamente sem invocar o handler.
     - As inconsistências são agrupadas por nome de propriedade na especialização `ValidationError` (que herda de `Error` com `ErrorType.Validation`).
     - Para comandos sem payload, retorna `Result.Failure(validationError)`.
     - Para comandos/queries tipados `Result<TValue>`, utiliza fábrica compilada via árvores de expressão com cache (`ConcurrentDictionary`) para invocar eficientemente `Result<TValue>.Failure(validationError)` sem overhead de reflexão em tempo de execução.
     - **Nenhuma exceção é lançada** para requests aderentes ao modelo `Result`.

3. **Injeção de Dependências Fluente (`MessagingServiceCollectionExtensions`):**
   - Método `services.AddMessaging(params Assembly[] assemblies)` para escaneamento e registro centralizado de handlers, notificações, behaviors abertos e validadores com descoberta automática.

## Consequências

### Positivas
- **Desacoplamento Rigoroso:** Módulos interagem exclusivamente via mensagens tipadas, permitindo futura extração para microsserviços se necessário.
- **Robustez e Previsibilidade:** Zero exceções de validação; toda falha flui deterministicamente pelos envelopes `Result` / `Result<T>`.
- **Granularidade de Validação:** A classe `ValidationError` expõe o dicionário estruturado `Errors` para resposta limpa a APIs REST e validações de formulário no Blazor.
- **Alta Performance:** Uso de expressões lambda compiladas e cacheadas para instanciação genérica de resultados.

### Negativas / Mitigações
- Exige que os desenvolvedores declarem seus comandos implementando as interfaces `ICommand` / `IQuery` do kernel em vez de `IRequest` cru do MediatR. Mitigado por documentação e testes automatizados.
