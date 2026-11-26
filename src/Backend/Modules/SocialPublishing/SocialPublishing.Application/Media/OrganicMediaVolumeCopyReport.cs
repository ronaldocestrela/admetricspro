namespace SocialPublishing.Application.Media;

/// <summary>
/// Resultado da cópia do volume local de mídia orgânica para o bucket (Fase 5).
/// </summary>
/// <param name="Copied">Objetos gravados ou substituídos no destino.</param>
/// <param name="Skipped">Objetos já presentes com o mesmo tamanho.</param>
/// <param name="Failed">Arquivos ou operações que falharam sem abortar o laço.</param>
/// <param name="MissingOnDisk">Chaves do SQL sem arquivo correspondente na raiz local.</param>
public sealed record OrganicMediaVolumeCopyReport(int Copied, int Skipped, int Failed, int MissingOnDisk = 0);