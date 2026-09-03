# ADR 0016: Documentação de Contratos OpenAPI v1 e Interface Interativa Scalar UI com Autenticação Corporativa

## Status
Aceito

## Contexto
O SaaS **AdMetricsPro** opera sob uma arquitetura de Monólito Modular com limites contextuais rigorosos (Bounded Contexts) e separação de bancos de dados por inquilino (*Database-per-Tenant*). A camada de WebApi expõe dezenas de operações de governança administrativa do Catálogo Master, tais como:
1. Gestão de planos comerciais, cotas e recursos (*Plans*).
2. Emissão e revogação de sessões de *Shadow Mode* com auditoria imutável (*Tenants / Impersonation*).
3. Monitoramento de cotas e rate limits de APIs de mídia com alertas preventivos de 80% (*ApiHealth*).
4. Régua de cobrança, suspensão e cancelamento progressivo (*Billing / Dunning*).
5. Chaveamento dinâmico e disjuntores operacionais de emergência (*FeatureFlags / Kill Switches*).

Para garantir que desenvolvedores, equipes de suporte, clientes de integração e ferramentas internas possam interagir com confiabilidade com o backend:
- Era mandatório possuir contratos OpenAPI 3.1 padronizados e sincronizados em tempo de execução com o código C#.
- Todos os endpoints deviam fornecer descrições operacionais legíveis (`[EndpointSummary]`), códigos de retorno semânticos (200, 201, 400, 404, 409, 422) e exemplos claros da estrutura do envelope `Result<T>`.
- A interface de documentação deveria suportar autenticação corporativa JWT Bearer diretamente pela interface web, facilitando testes seguros de APIs protegidas.

## Decisão

1. **Geração Nativa de OpenAPI no .NET 10 (`Microsoft.AspNetCore.OpenApi`):**
   - Utilização do pacote nativo `Microsoft.AspNetCore.OpenApi` e do modelo `Microsoft.OpenApi` v2.
   - Configuração de `AddDocumentTransformer` para enriquecer metadados do documento formal (título corporativo, versão, descrição, contato de engenharia e licença proprietária).
   - Inclusão do esquema de segurança `Bearer` (`OpenApiSecurityScheme`) com tipo `http`, scheme `bearer` e bearerFormat `JWT`.
   - Registro de requisito global de segurança via `OpenApiSecurityRequirement` com `OpenApiSecuritySchemeReference("Bearer", document)`.

2. **Interface Gráfica Interativa Moderna via Scalar UI (`Scalar.AspNetCore`):**
   - Exposição em `/scalar/v1` restrita por padrão aos ambientes `Development` e `Staging`.
   - Customização de tema visual (`ScalarTheme.Moon`) com alta legibilidade.
   - Pré-configuração do esquema preferencial de segurança via `.AddPreferredSecuritySchemes("Bearer")`, permitindo que o usuário informe o token JWT e execute testes interativos contra a API diretamente no navegador.

3. **Padronização Semântica de Respostas e Padrão `Result<T>`:**
   - Todo endpoint administrativo foi decorado com atributos explícitos `[EndpointSummary]` e `[ProducesResponseType]`.
   - Códigos de status padronizados:
     - `200 OK` / `201 Created`: Sucesso com envelope `Result<T>` serializado.
     - `400 BadRequest`: Parâmetros inválidos ou corpo ausente.
     - `404 NotFound`: Entidade de domínio não localizada.
     - `409 Conflict`: Violação de unicidade (ex: chave de flag duplicada).
     - `422 UnprocessableEntity`: Falhas em regras de negócio ou validações declaradas pelo `ErrorType.Validation`.

4. **Testes Automatizados de Aceitação (`OpenApiScalarEndpointTests`):**
   - Testes de integração em memória via `WebApplicationFactory<Program>` validando:
     - Disponibilidade e status 200 de `/openapi/v1.json` e `/scalar/v1`.
     - Presença do componente `components.securitySchemes.Bearer` e da seção `security`.
     - Presença de todos os 15 endpoints administrativos do Backoffice no catálogo OpenAPI.
     - Verificação automatizada de que 100% das operações HTTP declaradas possuem `summary` não vazio e respostas tipadas.

## Consequências

### Positivas
- **Contratos Vivos Sincronizados:** O catálogo OpenAPI reflete automaticamente qualquer novo comando ou consulta sem risco de obsolescência manual.
- **Experiência de Teste e Integração Acelerada:** Engenheiros e operadores podem autenticar no Scalar UI usando tokens emitidos pelo comando de impersonação ou JWT administrativo corporativo e disparar requisições em tempo real.
- **Conformidade Estrita com o AGENTS.md:** Atende com precisão às regras de documentação viva, OpenAPI nativo no .NET 10, padronização de códigos HTTP semânticos e eliminação de exceções de negócio.

### Negativas / Mitigações
- Documentação OpenAPI expõe a superfície de ataque caso habilitada em produção irrestrita.
  - *Mitigação:* O middleware `UseOpenApiAndScalar` está restrito via verificação de ambiente (`IsDevelopment() || IsEnvironment("Staging")`), bloqueando o acesso público em ambiente de produção.
