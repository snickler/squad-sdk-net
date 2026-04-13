using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Squad.SDK.NET.Platform;

namespace Squad.SDK.NET.Tests;

public sealed class TeamsCommunicationAdapterTests : IDisposable
{
    private readonly Stack<IDisposable> _snapshots = new();

    public void Dispose()
    {
        while (_snapshots.Count > 0)
            _snapshots.Pop().Dispose();
    }

    [Fact]
    public void LoadTokens_DifferentIdentityCaches_RemainIsolated()
    {
        var tenantA = UniqueId("tenant");
        var clientA = UniqueId("client");
        var tenantB = UniqueId("tenant");
        var clientB = UniqueId("client");
        var pathA = TeamsCommunicationAdapter.GetTokenPath(tenantA, clientA);
        var pathB = TeamsCommunicationAdapter.GetTokenPath(tenantB, clientB);
        TrackPath(pathA);
        TrackPath(pathB);

        var tokensA = CreateStoredTokens(tenantA, clientA, "auth-tenant-a", "auth-user-a", "refresh-a");
        var tokensB = CreateStoredTokens(tenantB, clientB, "auth-tenant-b", "auth-user-b", "refresh-b");
        WriteTokens(pathA, tokensA);
        WriteTokens(pathB, tokensB);

        var loadedA = TeamsCommunicationAdapter.LoadTokens(tenantA, clientA);
        var loadedB = TeamsCommunicationAdapter.LoadTokens(tenantB, clientB);

        Assert.NotEqual(pathA, pathB);
        Assert.StartsWith("teams-tokens-", Path.GetFileName(pathA));
        Assert.StartsWith("teams-tokens-", Path.GetFileName(pathB));
        Assert.NotNull(loadedA);
        Assert.NotNull(loadedB);
        Assert.Equal(tokensA.AccessToken, loadedA!.AccessToken);
        Assert.Equal(tokensA.RefreshToken, loadedA.RefreshToken);
        Assert.Equal(tokensB.AccessToken, loadedB!.AccessToken);
        Assert.Equal(tokensB.RefreshToken, loadedB.RefreshToken);
    }

    [Fact]
    public void MigrateLegacyTokens_MissingScopedCache_MovesLegacyTokensIntoIdentityScopedFile()
    {
        var tenantId = UniqueId("tenant");
        var clientId = UniqueId("client");
        var legacyPath = GetLegacyTokenPath();
        var scopedPath = TeamsCommunicationAdapter.GetTokenPath(tenantId, clientId);
        TrackPath(legacyPath);
        TrackPath(scopedPath);

        var legacyTokens = new StoredTokens
        {
            AccessToken = CreateAccessToken("legacy-auth-tenant", "legacy-auth-user"),
            RefreshToken = "legacy-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        WriteTokens(legacyPath, legacyTokens);

        TeamsCommunicationAdapter.MigrateLegacyTokens(tenantId, clientId);

        var migrated = TeamsCommunicationAdapter.LoadTokens(tenantId, clientId);

        Assert.False(File.Exists(legacyPath));
        Assert.True(File.Exists(scopedPath));
        Assert.NotNull(migrated);
        Assert.Equal("legacy-refresh", migrated!.RefreshToken);
        Assert.Equal(tenantId, migrated.ConfigTenantId);
        Assert.Equal(clientId, migrated.ClientId);
        Assert.Equal("legacy-auth-tenant", migrated.AuthenticatedTenantId);
        Assert.Equal("legacy-auth-user", migrated.AuthenticatedUserId);
    }

    [Fact]
    public void MigrateLegacyTokens_ExistingScopedCache_DropsLegacyFileWithoutOverwritingScopedTokens()
    {
        var tenantId = UniqueId("tenant");
        var clientId = UniqueId("client");
        var legacyPath = GetLegacyTokenPath();
        var scopedPath = TeamsCommunicationAdapter.GetTokenPath(tenantId, clientId);
        TrackPath(legacyPath);
        TrackPath(scopedPath);

        var scopedTokens = CreateStoredTokens(tenantId, clientId, "scoped-auth-tenant", "scoped-auth-user", "scoped-refresh");
        var legacyTokens = new StoredTokens
        {
            AccessToken = CreateAccessToken("legacy-auth-tenant", "legacy-auth-user"),
            RefreshToken = "legacy-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        WriteTokens(scopedPath, scopedTokens);
        WriteTokens(legacyPath, legacyTokens);

        TeamsCommunicationAdapter.MigrateLegacyTokens(tenantId, clientId);

        var migrated = TeamsCommunicationAdapter.LoadTokens(tenantId, clientId);

        Assert.False(File.Exists(legacyPath));
        Assert.NotNull(migrated);
        Assert.Equal(scopedTokens.AccessToken, migrated!.AccessToken);
        Assert.Equal(scopedTokens.RefreshToken, migrated.RefreshToken);
        Assert.Equal(scopedTokens.AuthenticatedTenantId, migrated.AuthenticatedTenantId);
        Assert.Equal(scopedTokens.AuthenticatedUserId, migrated.AuthenticatedUserId);
    }

    [Fact]
    public async Task LogoutAsync_CachedIdentityStatePresent_ClearsScopedTokensAndResetsIdentityCaches()
    {
        var tenantId = UniqueId("tenant");
        var clientId = UniqueId("client");
        var legacyPath = GetLegacyTokenPath();
        var scopedPath = TeamsCommunicationAdapter.GetTokenPath(tenantId, clientId);
        TrackPath(legacyPath);
        TrackPath(scopedPath);

        var tokens = CreateStoredTokens(tenantId, clientId, "logout-auth-tenant", "logout-auth-user", "logout-refresh");
        WriteTokens(scopedPath, tokens);

        var adapter = new TeamsCommunicationAdapter(
            new TeamsCommsConfig
            {
                TenantId = tenantId,
                ClientId = clientId,
                ChatId = "configured-chat"
            },
            new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("No HTTP requests expected during logout."))));

        SetPrivateField(adapter, "_tokens", tokens);
        SetPrivateField(adapter, "_resolvedChatId", "transient-chat");
        SetPrivateField(adapter, "_cachedUserId", "cached-user");

        await adapter.LogoutAsync();

        Assert.False(File.Exists(scopedPath));
        Assert.Null(GetPrivateField<StoredTokens?>(adapter, "_tokens"));
        Assert.Null(GetPrivateField<string?>(adapter, "_cachedUserId"));
        Assert.Equal("configured-chat", GetPrivateField<string?>(adapter, "_resolvedChatId"));
        Assert.Equal("https://teams.microsoft.com/l/chat/configured-chat", adapter.GetNotificationUrl("ignored-thread"));
    }

    [Fact]
    public async Task PostUpdateAsync_ExpiredCachedTokenRefreshes_PreservesExistingRefreshToken()
    {
        var tenantId = UniqueId("tenant");
        var clientId = UniqueId("client");
        var legacyPath = GetLegacyTokenPath();
        var scopedPath = TeamsCommunicationAdapter.GetTokenPath(tenantId, clientId);
        TrackPath(legacyPath);
        TrackPath(scopedPath);

        var staleTokens = CreateStoredTokens(
            tenantId,
            clientId,
            "stale-auth-tenant",
            "stale-auth-user",
            "original-refresh",
            DateTimeOffset.UtcNow.AddMinutes(-5));
        WriteTokens(scopedPath, staleTokens);

        var refreshedAccessToken = CreateAccessToken("fresh-auth-tenant", "fresh-auth-user");
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Url.Contains("/oauth2/v2.0/token", StringComparison.Ordinal))
            {
                Assert.Contains("grant_type=refresh_token", request.Body);
                return JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = refreshedAccessToken,
                    expires_in = 3600
                });
            }

            if (request.Url.Contains("/teams/team-123/channels/channel-456/messages", StringComparison.Ordinal))
            {
                Assert.Equal($"Bearer {refreshedAccessToken}", request.Authorization);
                return JsonResponse(HttpStatusCode.Created, new { id = "message-1" });
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.Url}");
        });

        var adapter = new TeamsCommunicationAdapter(
            new TeamsCommsConfig
            {
                TenantId = tenantId,
                ClientId = clientId,
                TeamId = "team-123",
                ChannelId = "channel-456"
            },
            new HttpClient(handler));

        var result = await adapter.PostUpdateAsync(new PostUpdateOptions
        {
            Title = "Status",
            Body = "Refresh path",
            Author = "Drummer"
        });

        var persisted = TeamsCommunicationAdapter.LoadTokens(tenantId, clientId);

        Assert.Equal("team-123|channel-456", result.Id);
        Assert.Equal("https://teams.microsoft.com/l/channel/channel-456", result.Url);
        Assert.Equal(2, handler.Requests.Count);
        Assert.NotNull(persisted);
        Assert.Equal("original-refresh", persisted!.RefreshToken);
        Assert.Equal(refreshedAccessToken, persisted.AccessToken);
        Assert.Equal(tenantId, persisted.ConfigTenantId);
        Assert.Equal(clientId, persisted.ClientId);
        Assert.Equal("fresh-auth-tenant", persisted.AuthenticatedTenantId);
        Assert.Equal("fresh-auth-user", persisted.AuthenticatedUserId);
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_InvalidGrant_ReturnsPermanentAuthErrorCodeForCacheEviction()
    {
        var tenantId = UniqueId("tenant");
        var clientId = UniqueId("client");
        TrackPath(GetLegacyTokenPath());

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Url.Contains("/oauth2/v2.0/token", StringComparison.Ordinal))
            {
                return JsonResponse(HttpStatusCode.BadRequest, new
                {
                    error = "invalid_grant",
                    error_description = "refresh token revoked"
                });
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.Url}");
        });

        var adapter = new TeamsCommunicationAdapter(
            new TeamsCommsConfig
            {
                TenantId = tenantId,
                ClientId = clientId
            },
            new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<TeamsAuthException>(() =>
            InvokePrivateAsync<StoredTokens>(adapter, "RefreshAccessTokenAsync", "stale-refresh", CancellationToken.None));

        Assert.Equal("invalid_grant", exception.ErrorCode);
        Assert.Contains("invalid_grant", exception.Message);
    }

    private void TrackPath(string path)
    {
        _snapshots.Push(new DirectorySnapshot(Path.GetDirectoryName(path)!));
        _snapshots.Push(new FileSnapshot(path));
    }

    private static string UniqueId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static string GetLegacyTokenPath()
    {
        var probePath = TeamsCommunicationAdapter.GetTokenPath("legacy-probe-tenant", "legacy-probe-client");
        return Path.Combine(Path.GetDirectoryName(probePath)!, "teams-tokens.json");
    }

    private static StoredTokens CreateStoredTokens(
        string tenantId,
        string clientId,
        string authenticatedTenantId,
        string authenticatedUserId,
        string refreshToken,
        DateTimeOffset? expiresAt = null)
        => new()
        {
            AccessToken = CreateAccessToken(authenticatedTenantId, authenticatedUserId),
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
            ConfigTenantId = tenantId,
            ClientId = clientId,
            AuthenticatedTenantId = authenticatedTenantId,
            AuthenticatedUserId = authenticatedUserId
        };

    private static void WriteTokens(string path, StoredTokens tokens)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(tokens, TeamsJsonContext.Default.StoredTokens);
        File.WriteAllText(path, json);
    }

    private static string CreateAccessToken(string? tenantId, string? userId)
    {
        var payload = new Dictionary<string, string?>();
        if (tenantId is not null)
            payload["tid"] = tenantId;
        if (userId is not null)
            payload["oid"] = userId;

        return $"{Base64UrlEncode("{\"alg\":\"none\"}")}.{Base64UrlEncode(JsonSerializer.Serialize(payload))}.signature";
    }

    private static string Base64UrlEncode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found.");

        return (T)field.GetValue(instance)!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found.");

        field.SetValue(instance, value);
    }

    private static async Task<T> InvokePrivateAsync<T>(object instance, string methodName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");

        var task = (Task<T>)method.Invoke(instance, arguments)!;
        return await task;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object payload)
        => new(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    private sealed class DirectorySnapshot : IDisposable
    {
        private readonly string _path;
        private readonly bool _existed;

        public DirectorySnapshot(string path)
        {
            _path = path;
            _existed = Directory.Exists(path);
        }

        public void Dispose()
        {
            if (_existed || !Directory.Exists(_path))
                return;

            if (!Directory.EnumerateFileSystemEntries(_path).Any())
                Directory.Delete(_path);
        }
    }

    private sealed class FileSnapshot : IDisposable
    {
        private readonly string _path;
        private readonly byte[]? _originalContent;
        private readonly bool _existed;

        public FileSnapshot(string path)
        {
            _path = path;
            if (File.Exists(path))
            {
                _existed = true;
                _originalContent = File.ReadAllBytes(path);
            }
        }

        public void Dispose()
        {
            if (_existed)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllBytes(_path, _originalContent!);
                return;
            }

            if (File.Exists(_path))
                File.Delete(_path);
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Url, string? Authorization, string Body);

    private sealed class StubHttpMessageHandler(Func<CapturedRequest, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var captured = new CapturedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                request.Headers.Authorization?.ToString(),
                body);
            Requests.Add(captured);
            return handler(captured);
        }
    }
}
