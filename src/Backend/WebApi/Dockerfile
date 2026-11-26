# ==============================================================================
# Dockerfile - AdMetricsPro WebApi (.NET 10)
# ==============================================================================
# Contexto de compilação: raiz do repositório
# ==============================================================================

# ------------------------------------------------------------------------------
# 1. Estágio de Compilação (Build & Publish)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia arquivos de configuração e códigos-fonte do projeto
COPY Directory.Build.props ./
COPY AdMetricsPro.sln ./
COPY src/ src/

# Restaura e publica a WebApi em modo Release
RUN dotnet restore src/Backend/WebApi/WebApi.csproj
RUN dotnet publish src/Backend/WebApi/WebApi.csproj -c Release -o /app/publish --no-restore

# ------------------------------------------------------------------------------
# 2. Estágio Final de Execução (Runtime .NET 10 Enxuto)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Instala curl para healthchecks em containers.
# O processo inicia como root só para alinhar a posse dos volumes de mídia
# (criados pelo daemon como root) e o entrypoint troca para o usuário app.
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data/social-media /app/data/object-storage \
    && chown -R app:app /app/data

COPY src/Backend/WebApi/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod 755 /usr/local/bin/docker-entrypoint.sh

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["docker-entrypoint.sh"]
