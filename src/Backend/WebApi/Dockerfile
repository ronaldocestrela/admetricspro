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

# Instala curl para healthchecks em containers
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Executa com usuário não-root nativo do .NET
USER app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebApi.dll"]
