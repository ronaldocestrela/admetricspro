using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;

namespace WebApp.Services;

/// <summary>Cliente tipado da operação Meta; nunca recebe tokens da plataforma.</summary>
public interface IMetaAdsClientService
{
    /// <summary>Pesquisa opções oficiais de segmentação da conta autorizada.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="type">Tipo oficial de opção.</param>
    /// <param name="query">Termo pesquisado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Opções fornecidas pela Meta.</returns>
    Task<Result<IReadOnlyList<JsonElement>>> SearchTargetingAsync(Guid workspaceId, Guid accountId, string type, string query, CancellationToken cancellationToken = default);
    /// <summary>Consulta o processamento de um vídeo enviado por este workspace.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="operationId">Operação de upload do vídeo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Estado oficial do processamento.</returns>
    Task<Result<MetaMediaResult>> GetVideoStatusAsync(Guid workspaceId, Guid accountId, Guid operationId, CancellationToken cancellationToken = default);
    /// <summary>Sincroniza hierarquia e métricas usando as rotas compartilhadas.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="since">Primeiro dia.</param>
    /// <param name="until">Último dia.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resumo da sincronização estrutural.</returns>
    Task<Result<SyncCampaignHierarchySummaryDto>> SyncAsync(Guid workspaceId, Guid accountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
    /// <summary>Consulta Insights detalhados.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="filter">Filtros.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Métricas opcionais por linha.</returns>
    Task<Result<IReadOnlyList<MetaInsight>>> GetInsightsAsync(Guid workspaceId, Guid accountId, MetaInsightsFilter filter, CancellationToken cancellationToken = default);
    /// <summary>Cria ou altera recurso com idempotência.</summary>
    /// <param name="request">Pedido validado também pelo backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Objeto externo confirmado.</returns>
    Task<Result<MetaWriteResult>> WriteAsync(WriteMetaResourceCommand request, CancellationToken cancellationToken = default);
    /// <summary>Consulta detalhes e estado efetivo.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="resource">Tipo de recurso.</param>
    /// <param name="externalId">ID externo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Campos oficiais do objeto.</returns>
    Task<Result<JsonElement>> GetResourceAsync(Guid workspaceId, Guid accountId, string resource, string externalId, CancellationToken cancellationToken = default);
    /// <summary>Lista ativos elegíveis para criativos e segmentação.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="asset">Tipo de ativo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Ativos oficiais.</returns>
    Task<Result<IReadOnlyList<JsonElement>>> GetAssetsAsync(Guid workspaceId, Guid accountId, string asset, CancellationToken cancellationToken = default);
    /// <summary>Envia mídia sem publicar anúncios.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="operationId">Chave idempotente do arquivo.</param>
    /// <param name="name">Nome do arquivo.</param>
    /// <param name="contentType">Tipo MIME.</param>
    /// <param name="content">Bytes limitados.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Hash de imagem ou ID do vídeo.</returns>
    Task<Result<MetaMediaResult>> UploadAsync(Guid workspaceId, Guid accountId, Guid operationId, string name, string contentType, byte[] content, CancellationToken cancellationToken = default);
}
