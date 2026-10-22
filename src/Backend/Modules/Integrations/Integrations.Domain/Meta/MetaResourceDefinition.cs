using System.Text.Json;

namespace Integrations.Domain.Meta;

/// <summary>Catálogo versionado de parâmetros públicos de um recurso Meta.</summary>
/// <param name="Resource">Recurso da API.</param>
/// <param name="ApiVersion">Versão Graph analisada.</param>
/// <param name="SdkVersion">Versão da referência oficial.</param>
/// <param name="SourceCommit">Commit da referência oficial.</param>
/// <param name="Fields">Campos de criação e edição, incluindo tipos e restrições.</param>
public sealed record MetaResourceDefinition(string Resource, string ApiVersion, string SdkVersion,
    string SourceCommit, IReadOnlyList<MetaResourceField> Fields);

/// <summary>Campo oficial com controles de apresentação e disponibilidade por operação.</summary>
/// <param name="Name">Nome oficial do parâmetro.</param>
/// <param name="Label">Rótulo de apresentação.</param>
/// <param name="Group">Assunto do formulário.</param>
/// <param name="Description">Ajuda e condições da opção.</param>
/// <param name="Type">Tipo declarado no SDK oficial.</param>
/// <param name="Create">Suporte no endpoint de criação.</param>
/// <param name="Update">Suporte no endpoint de edição.</param>
/// <param name="Readable">Campo consultável do objeto.</param>
/// <param name="EnumValues">Valores declarados na referência oficial.</param>
/// <param name="Restriction">Motivo explícito de uma opção reservada ou somente de leitura.</param>
/// <param name="Children">Estrutura documentada de objetos e listas aninhadas.</param>
public sealed record MetaResourceField(string Name, string Label, string Group, string Description, string Type,
    bool Create, bool Update, bool Readable, IReadOnlyList<string> EnumValues, string? Restriction,
    IReadOnlyList<MetaResourceField> Children);

/// <summary>Configuração remota com indicação explícita de campos não disponíveis.</summary>
/// <param name="Values">Valores atuais obtidos da Meta.</param>
/// <param name="UnavailableFields">Campo e erro sanitizado de leitura, sem substituir o valor por um padrão.</param>
public sealed record MetaResourceConfiguration(JsonElement Values, IReadOnlyDictionary<string, string> UnavailableFields);
