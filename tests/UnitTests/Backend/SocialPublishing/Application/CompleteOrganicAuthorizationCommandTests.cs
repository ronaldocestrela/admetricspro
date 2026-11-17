using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Infrastructure.OAuth;
using SocialPublishing.Infrastructure.Security;
using UnitTests.Backend.Common;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes unitários do comando <see cref="CompleteOrganicAuthorizationCommandHandler"/>.
/// </summary>
public sealed class CompleteOrganicAuthorizationCommandTests
{
    private const string TestSigningKey = "v3ryS3cur3AndLong3n0ughK3yForHmacSha256Signature!12345";
    private const string RedirectUri = "https://app.example.com/social/callback";
    private const string PlainAccessToken = "PLAIN_ACCESS_TOKEN_SECRET";
    private const string AuthCode = "auth_code_secret";
    private const string ClientSecret = "meta-client-secret-value";

    private static readonly string EncryptionKey = Convert.ToBase64String(new byte[32]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32
    });

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly InMemoryOrganicConnectionRepository _repository = new();
    private readonly ISocialPublishingUnitOfWork _unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
    private readonly OrganicOAuthOptions _options = new()
    {
        StateSigningKey = TestSigningKey,
        Meta = new MetaOrganicOAuthOptions
        {
            AppId = "meta-app-id",
            AppSecret = ClientSecret,
            GraphApiVersion = "v26.0",
            InstagramScopes = "pages_show_list,instagram_content_publish",
        },
    };

    /// <summary>
    /// State válido e código aceito deve cifrar o token e permitir decifrar com o propósito correto.
    /// </summary>
    [Fact]
    public async Task Handle_ComStateValidoECodigoAceito_DevePersistirCiphertextRecuperavel()
    {
        var httpCalls = new HttpCallCounter();
        var handler = CreateHandler(() => CreateMetaHttpClient(httpCalls));
        var state = IssueState(OrganicPlatform.Instagram);

        var result = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        httpCalls.Count.Should().BeGreaterThan(0);
        _repository.Items.Should().ContainSingle();
        var connection = _repository.Items[0];
        connection.EncryptedAccessToken.Should().NotBe(PlainAccessToken);

        var protector = new OrganicTokenProtector(new AesEncryptionService(EncryptionKey));
        var purpose = OrganicConnection.EncryptionPurpose(
            OrganicPlatform.Instagram,
            _workspaceId,
            connection.Id);
        var unprotect = protector.Unprotect(connection.EncryptedAccessToken, purpose);
        unprotect.IsSuccess.Should().BeTrue();
        unprotect.Value.Should().Be(PlainAccessToken);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Decifrar com propósito de outro workspace deve falhar sem vazar o segredo.
    /// </summary>
    [Fact]
    public async Task Unprotect_ComPropositoDeOutroWorkspace_DeveRetornarVaultPurposeMismatch()
    {
        var handler = CreateHandler(() => CreateMetaHttpClient());
        var state = IssueState(OrganicPlatform.Instagram);
        var result = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var connection = _repository.Items[0];
        var protector = new OrganicTokenProtector(new AesEncryptionService(EncryptionKey));
        var wrongPurpose = OrganicConnection.EncryptionPurpose(
            OrganicPlatform.Instagram,
            Guid.NewGuid(),
            connection.Id);

        var unprotect = protector.Unprotect(connection.EncryptedAccessToken, wrongPurpose);
        unprotect.IsFailure.Should().BeTrue();
        unprotect.Error.Code.Should().Be(SocialErrors.VaultPurposeMismatch.Code);
        unprotect.Error.Description.Should().NotContain(PlainAccessToken);
    }

    /// <summary>
    /// State inválido não deve chamar a rede externa.
    /// </summary>
    [Fact]
    public async Task Handle_ComStateInvalido_NaoDeveChamarHttp()
    {
        var httpCalls = new HttpCallCounter();
        var handler = CreateHandler(() => CreateMetaHttpClient(httpCalls));

        var result = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, "invalid.state.token", RedirectUri),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
        httpCalls.Count.Should().Be(0);
        _repository.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Falha HTTP do provedor deve retornar Social.OAuthExchangeFailed sem exceção.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoProvedorRejeitaCodigo_DeveRetornarOAuthExchangeFailed()
    {
        var handler = CreateHandler(() => new HttpClient(new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest))));
        var state = IssueState(OrganicPlatform.Instagram);

        var result = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state, RedirectUri),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthExchangeFailed.Code);
    }

    /// <summary>
    /// Segunda conclusão para a mesma conta externa deve reutilizar o ConnectionId.
    /// </summary>
    [Fact]
    public async Task Handle_SegundaConclusaoMesmaConta_DeveReutilizarConnectionId()
    {
        var handler = CreateHandler(() => CreateMetaHttpClient());
        var state1 = IssueState(OrganicPlatform.Instagram);
        var first = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state1, RedirectUri),
            CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        var state2 = IssueState(OrganicPlatform.Instagram);
        var second = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state2, RedirectUri),
            CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.ConnectionId.Should().Be(first.Value.ConnectionId);
        _repository.Items.Should().ContainSingle();
    }

    /// <summary>
    /// Nenhuma mensagem de log do handler deve conter segredos OAuth de teste.
    /// </summary>
    [Fact]
    public async Task Handle_NaoDeveRegistrarSegredosNoLogger()
    {
        var logger = new SecretCapturingLogger<CompleteOrganicAuthorizationCommandHandler>();
        var handler = CreateHandler(() => CreateMetaHttpClient(), logger);
        var state = IssueState(OrganicPlatform.Instagram);

        var result = await handler.Handle(
            new CompleteOrganicAuthorizationCommand(_workspaceId, AuthCode, state, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        logger.Messages.Should().NotContain(m =>
            m.Contains(PlainAccessToken, StringComparison.Ordinal) ||
            m.Contains(AuthCode, StringComparison.Ordinal) ||
            m.Contains(ClientSecret, StringComparison.Ordinal));
    }

    private CompleteOrganicAuthorizationCommandHandler CreateHandler(
        Func<HttpClient> httpClientFactory,
        ILogger<CompleteOrganicAuthorizationCommandHandler>? logger = null)
    {
        var tenantAccessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, null, TenantResolutionSource.Header, _tenantId.ToString()),
        };

        var stateService = new OrganicOAuthStateService(TestSigningKey);
        var httpClient = httpClientFactory();
        var adapter = new MetaInstagramOrganicAdapter(
            httpClient,
            Options.Create(_options));

        var protector = new OrganicTokenProtector(new AesEncryptionService(EncryptionKey));

        return new CompleteOrganicAuthorizationCommandHandler(
            tenantAccessor,
            stateService,
            new IOrganicPlatformAdapter[] { adapter },
            protector,
            _repository,
            _unitOfWork,
            logger ?? NullLogger<CompleteOrganicAuthorizationCommandHandler>.Instance);
    }

    private string IssueState(OrganicPlatform platform)
    {
        var stateService = new OrganicOAuthStateService(TestSigningKey);
        return stateService.Issue(_tenantId, _workspaceId, platform, RedirectUri, DateTime.UtcNow).State;
    }

    private static HttpClient CreateMetaHttpClient(HttpCallCounter? counter = null) =>
        new(new TestHttpMessageHandler((req, _) =>
        {
            if (counter is not null)
            {
                counter.Count++;
            }

            return CreateMetaResponse(req);
        }));

    private static HttpClient CreateMetaHttpClient() =>
        new(new TestHttpMessageHandler((req, _) => CreateMetaResponse(req)));

    private static HttpResponseMessage CreateMetaResponse(HttpRequestMessage req)
    {
        var url = req.RequestUri!.ToString();
        if (url.Contains("oauth/access_token", StringComparison.Ordinal) && url.Contains("code=", StringComparison.Ordinal))
        {
            return JsonResponse(new { access_token = "short-lived-user-token" });
        }

        if (url.Contains("fb_exchange_token", StringComparison.Ordinal))
        {
            return JsonResponse(new { access_token = "long-lived-user-token", expires_in = 3600 });
        }

        if (url.Contains("me/accounts", StringComparison.Ordinal))
        {
            var payload = JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new
                    {
                        id = "page-1",
                        name = "Brand Page",
                        access_token = PlainAccessToken,
                        instagram_business_account = new { id = "ig-ext-001", username = "brand" },
                    },
                },
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage JsonResponse(object body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

    private sealed class InMemoryOrganicConnectionRepository : IOrganicConnectionRepository
    {
        public List<OrganicConnection> Items { get; } = new();

        public Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c => c.Id == id));

        public Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c =>
                c.WorkspaceId == workspaceId &&
                c.Platform == platform &&
                c.ExternalAccountId == externalAccountId));

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(Items.Where(c => c.WorkspaceId == workspaceId).ToList());

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            Items.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            var index = Items.FindIndex(c => c.Id == connection.Id);
            if (index >= 0)
            {
                Items[index] = connection;
            }

            return Task.CompletedTask;
        }

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(c => c.WorkspaceId == workspaceId));
    }

    private sealed class HttpCallCounter
    {
        public int Count { get; set; }
    }

    private sealed class SecretCapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
