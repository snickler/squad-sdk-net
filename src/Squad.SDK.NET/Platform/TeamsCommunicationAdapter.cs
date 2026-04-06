using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Squad.SDK.NET.Platform;

/// <summary>
/// Configuration for the Microsoft Teams communication adapter.
/// </summary>
public sealed record TeamsCommsConfig
{
    /// <summary>Gets the Entra tenant ID. Defaults to <c>"organizations"</c> (multi-tenant).</summary>
    public string TenantId { get; init; } = "organizations";
    /// <summary>
    /// Gets the Entra application (client) ID.
    /// Defaults to the Microsoft Graph PowerShell first-party app ID which works
    /// in every Microsoft tenant without a custom Entra registration.
    /// </summary>
    public string ClientId { get; init; } = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
    /// <summary>Gets the recipient UPN (e.g. <c>"user@contoso.com"</c>) or <c>"me"</c> for self-chat.</summary>
    public string? RecipientUpn { get; init; }
    /// <summary>Gets an existing Teams chat ID (skips chat creation when set).</summary>
    public string? ChatId { get; init; }
    /// <summary>Gets a Teams channel ID as an alternative to 1:1 chat.</summary>
    public string? ChannelId { get; init; }
    /// <summary>Gets the Teams team ID, required when <see cref="ChannelId"/> is set.</summary>
    public string? TeamId { get; init; }
}

/// <summary>
/// Microsoft Teams communication adapter — bidirectional chat via Microsoft Graph API.
/// </summary>
/// <remarks>
/// <para>
/// Auth priority: cached token → refresh → device code flow.
/// Tokens are stored in <c>{personalSquadDir}/teams-tokens.json</c> with restricted permissions.
/// </para>
/// <para>
/// Uses the Microsoft Graph PowerShell first-party client ID by default, which works
/// in every Microsoft tenant without requiring a custom Entra app registration.
/// </para>
/// </remarks>
public sealed class TeamsCommunicationAdapter : ICommunicationAdapter
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string GraphScopes = "Chat.ReadWrite ChatMessage.Send ChatMessage.Read User.Read offline_access";

    private readonly TeamsCommsConfig _config;
    private readonly ILogger<TeamsCommunicationAdapter> _logger;
    private readonly HttpClient _httpClient;

    /// <inheritdoc/>
    public CommunicationChannel Channel => CommunicationChannel.TeamsGraph;

    /// <summary>
    /// Initializes a new <see cref="TeamsCommunicationAdapter"/>.
    /// </summary>
    /// <param name="config">Teams-specific configuration.</param>
    /// <param name="logger">Optional logger; defaults to a no-op logger.</param>
    /// <param name="httpClient">Optional HTTP client; a new instance is created when <see langword="null"/>.</param>
    public TeamsCommunicationAdapter(
        TeamsCommsConfig? config = null,
        ILogger<TeamsCommunicationAdapter>? logger = null,
        HttpClient? httpClient = null)
    {
        _config = config ?? new TeamsCommsConfig();
        _logger = logger ?? NullLogger<TeamsCommunicationAdapter>.Instance;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public async Task<PostUpdateResult> PostUpdateAsync(PostUpdateOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var chatId = await EnsureChatAsync(accessToken, cancellationToken).ConfigureAwait(false);

        var htmlBody = FormatTeamsMessage(options.Title, options.Body, options.Author);
        var payload = new TeamsChatMessagePayload
        {
            Body = new TeamsMessageBody { ContentType = "html", Content = htmlBody }
        };

        var jsonPayload = JsonSerializer.Serialize(payload, TeamsJsonContext.Default.TeamsChatMessagePayload);
        var response = await GraphPostJsonAsync(
            $"{GraphBase}/chats/{ValidateGraphId(chatId, "chatId")}/messages",
            accessToken,
            jsonPayload,
            cancellationToken).ConfigureAwait(false);

        var messageId = response is not null && response.TryGetValue("id", out var idElem)
            ? idElem.GetString() ?? string.Empty
            : string.Empty;
        var chatUrl = $"https://teams.microsoft.com/l/message/{Uri.EscapeDataString(chatId)}/{Uri.EscapeDataString(messageId)}";

        _logger.LogInformation("Posted Teams message {MessageId} to chat {ChatId}", messageId, chatId);
        return new PostUpdateResult { Id = messageId, Url = chatUrl };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CommunicationReply>> PollForRepliesAsync(
        PollForRepliesOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var chatId = await EnsureChatAsync(accessToken, cancellationToken).ConfigureAwait(false);

        var url = $"{GraphBase}/chats/{ValidateGraphId(chatId, "chatId")}/messages" +
                  $"?$filter=createdDateTime gt {options.Since:O}&$orderby=createdDateTime asc";

        var result = await GraphGetAsync(url, accessToken, cancellationToken).ConfigureAwait(false);
        var messages = result?["value"] as JsonElement? ?? default;

        var replies = new List<CommunicationReply>();
        if (messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var msg in messages.EnumerateArray())
            {
                var msgId = msg.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                var createdStr = msg.TryGetProperty("createdDateTime", out var createdProp) ? createdProp.GetString() : null;
                var timestamp = createdStr is not null && DateTimeOffset.TryParse(createdStr, out var ts) ? ts : DateTimeOffset.UtcNow;

                var from = msg.TryGetProperty("from", out var fromProp)
                    ? fromProp.TryGetProperty("user", out var userProp)
                        ? userProp.TryGetProperty("displayName", out var nameProp) ? nameProp.GetString() ?? "unknown" : "unknown"
                        : "unknown"
                    : "unknown";

                var bodyContent = msg.TryGetProperty("body", out var bodyProp)
                    ? bodyProp.TryGetProperty("content", out var contentProp) ? StripHtml(contentProp.GetString() ?? "") : ""
                    : "";

                // Skip messages sent by the bot/app itself (empty body or system messages)
                if (string.IsNullOrWhiteSpace(bodyContent))
                    continue;

                replies.Add(new CommunicationReply
                {
                    Id = msgId,
                    Author = from,
                    Body = bodyContent,
                    Timestamp = timestamp
                });
            }
        }

        return replies.AsReadOnly();
    }

    /// <inheritdoc/>
    public string? GetNotificationUrl(string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
            return null;

        // Deep-link to the Teams chat
        return $"https://teams.microsoft.com/l/chat/{Uri.EscapeDataString(threadId)}/0";
    }

    // =========================================================================
    // Token management
    // =========================================================================

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tokenPath = GetTokenPath();
        var stored = LoadTokens(tokenPath);

        if (stored is not null)
        {
            // Use cached token if still valid (with 60-second buffer)
            if (DateTimeOffset.UtcNow.AddSeconds(60) < stored.ExpiresAt)
                return stored.AccessToken;

            // Try to refresh
            try
            {
                var refreshed = await RefreshTokenAsync(stored.RefreshToken, cancellationToken).ConfigureAwait(false);
                SaveTokens(tokenPath, refreshed);
                return refreshed.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed; falling back to device code flow");
            }
        }

        // Device code flow
        var tokens = await StartDeviceCodeFlowAsync(cancellationToken).ConfigureAwait(false);
        SaveTokens(tokenPath, tokens);
        return tokens.AccessToken;
    }

    private async Task<StoredTokens> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenEndpoint = $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token";
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["scope"] = GraphScopes,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseTokenResponse(json);
    }

    private async Task<StoredTokens> StartDeviceCodeFlowAsync(CancellationToken cancellationToken)
    {
        var deviceCodeEndpoint = $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/devicecode";
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["scope"] = GraphScopes
        };

        using var codeRequest = new HttpRequestMessage(HttpMethod.Post, deviceCodeEndpoint)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        var codeResponse = await _httpClient.SendAsync(codeRequest, cancellationToken).ConfigureAwait(false);
        codeResponse.EnsureSuccessStatusCode();

        var codeJson = await codeResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var codeDoc = JsonDocument.Parse(codeJson);
        var root = codeDoc.RootElement;

        var userCode = root.TryGetProperty("user_code", out var ucProp) ? ucProp.GetString() ?? "" : "";
        var deviceCode = root.TryGetProperty("device_code", out var dcProp) ? dcProp.GetString() ?? "" : "";
        var verificationUri = root.TryGetProperty("verification_uri", out var vuProp) ? vuProp.GetString() ?? "" : "";
        var interval = root.TryGetProperty("interval", out var intervalProp) ? intervalProp.GetInt32() : 5;
        var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "";

        // Emit the device code message via logger so applications can capture/display it
        _logger.LogWarning("Teams auth required: {Message}", message);

        // Poll for token
        var tokenEndpoint = $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token";
        var deadline = DateTimeOffset.UtcNow.AddMinutes(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken).ConfigureAwait(false);

            var pollData = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = deviceCode
            };

            using var pollRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(pollData)
            };
            var pollResponse = await _httpClient.SendAsync(pollRequest, cancellationToken).ConfigureAwait(false);
            var pollJson = await pollResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (pollResponse.IsSuccessStatusCode)
                return ParseTokenResponse(pollJson);

            using var errorDoc = JsonDocument.Parse(pollJson);
            var errorCode = errorDoc.RootElement.TryGetProperty("error", out var errProp)
                ? errProp.GetString()
                : null;

            if (errorCode == "authorization_pending" || errorCode == "slow_down")
            {
                if (errorCode == "slow_down")
                    interval += 5;
                continue;
            }

            throw new InvalidOperationException($"Device code flow failed: {errorCode}");
        }

        throw new TimeoutException("Teams authentication timed out waiting for user action.");
    }

    // =========================================================================
    // Graph API helpers
    // =========================================================================

    private async Task<string> EnsureChatAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_config.ChatId))
            return _config.ChatId;

        if (!string.IsNullOrEmpty(_config.ChannelId) && !string.IsNullOrEmpty(_config.TeamId))
            return $"{_config.TeamId}/channels/{_config.ChannelId}";

        if (!string.IsNullOrEmpty(_config.RecipientUpn))
        {
            // Create or find a 1:1 chat with the recipient
            var myId = await GetMyUserIdAsync(accessToken, cancellationToken).ConfigureAwait(false);
            if (myId is null)
                throw new InvalidOperationException("Could not determine the current user's Teams ID.");

            string recipientId;
            if (string.Equals(_config.RecipientUpn, "me", StringComparison.OrdinalIgnoreCase))
            {
                recipientId = myId;
            }
            else
            {
                var userResponse = await GraphGetAsync(
                    $"{GraphBase}/users/{Uri.EscapeDataString(_config.RecipientUpn)}?$select=id",
                    accessToken, cancellationToken).ConfigureAwait(false);
                recipientId = userResponse is not null && userResponse.TryGetValue("id", out var uidElem)
                    ? uidElem.GetString() ?? throw new InvalidOperationException($"Could not find Teams user: {_config.RecipientUpn}")
                    : throw new InvalidOperationException($"Could not find Teams user: {_config.RecipientUpn}");
            }

            var chatPayload = new TeamsChatPayload
            {
                ChatType = "oneOnOne",
                Members =
                [
                    new TeamsConversationMember
                    {
                        OdataType = "#microsoft.graph.aadUserConversationMember",
                        Roles = ["owner"],
                        UserId = FormatUserIdUrl(ValidateGraphId(myId, "myId"))
                    },
                    new TeamsConversationMember
                    {
                        OdataType = "#microsoft.graph.aadUserConversationMember",
                        Roles = ["owner"],
                        UserId = FormatUserIdUrl(ValidateGraphId(recipientId, "recipientId"))
                    }
                ]
            };

            var chatJson = JsonSerializer.Serialize(chatPayload, TeamsJsonContext.Default.TeamsChatPayload);
            var chatResponse = await GraphPostJsonAsync($"{GraphBase}/chats", accessToken, chatJson, cancellationToken).ConfigureAwait(false);
            return chatResponse is not null && chatResponse.TryGetValue("id", out var chatIdElem)
                ? chatIdElem.GetString() ?? throw new InvalidOperationException("Failed to create or retrieve Teams chat.")
                : throw new InvalidOperationException("Failed to create or retrieve Teams chat.");
        }

        throw new InvalidOperationException(
            "TeamsCommsConfig requires at least one of: ChatId, ChannelId+TeamId, or RecipientUpn.");
    }

    private async Task<string?> GetMyUserIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        var result = await GraphGetAsync($"{GraphBase}/me?$select=id", accessToken, cancellationToken).ConfigureAwait(false);
        return result is not null && result.TryGetValue("id", out var idElem) ? idElem.GetString() : null;
    }

    private async Task<Dictionary<string, JsonElement>?> GraphGetAsync(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        const int MaxRetries = 3;
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning("Graph API throttled, retrying in {Seconds}s (attempt {Attempt})", retryAfter.TotalSeconds, attempt + 1);
                await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        return null;
    }

    private async Task<Dictionary<string, JsonElement>?> GraphPostJsonAsync(
        string url,
        string accessToken,
        string jsonPayload,
        CancellationToken cancellationToken)
    {
        const int MaxRetries = 3;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning("Graph API throttled, retrying in {Seconds}s (attempt {Attempt})", retryAfter.TotalSeconds, attempt + 1);
                await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return null;
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        return null;
    }

    // =========================================================================
    // Token persistence
    // =========================================================================

    private static string GetTokenPath()
    {
        var personalDir = Resolution.SquadResolver.ResolvePersonalSquadDir()
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".squad");
        Directory.CreateDirectory(personalDir);
        return Path.Combine(personalDir, "teams-tokens.json");
    }

    private static StoredTokens? LoadTokens(string tokenPath)
    {
        try
        {
            if (!File.Exists(tokenPath))
                return null;
            var raw = File.ReadAllText(tokenPath);
            return JsonSerializer.Deserialize(raw, TeamsJsonContext.Default.StoredTokens);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveTokens(string tokenPath, StoredTokens tokens)
    {
        var json = JsonSerializer.Serialize(tokens, TeamsJsonContext.Default.StoredTokens);
        File.WriteAllText(tokenPath, json);

        // Restrict file permissions on Unix
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                // Set permissions to 600 (owner read/write only) using chmod
                var dir = Path.GetDirectoryName(tokenPath);
                if (dir is not null)
                    File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.SetUnixFileMode(tokenPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // Best-effort — not all platforms support this
            }
        }
    }

    private static StoredTokens ParseTokenResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var atProp) ? atProp.GetString() ?? "" : "";
        var refreshToken = root.TryGetProperty("refresh_token", out var rtProp) ? rtProp.GetString() ?? "" : "";
        var expiresIn = root.TryGetProperty("expires_in", out var eiProp) ? eiProp.GetInt32() : 3600;

        return new StoredTokens
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        };
    }

    // =========================================================================
    // Message formatting helpers
    // =========================================================================

    private static string FormatTeamsMessage(string title, string body, string? author)
    {
        var authorSuffix = author is not null ? $" <em>— {EscapeHtml(author)}</em>" : "";
        return $"<h3>{EscapeHtml(title)}</h3><p>{EscapeHtml(body)}{authorSuffix}</p>";
    }

    internal static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    internal static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);

    private static string ValidateGraphId(string id, string label)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('/') || id.Contains('\0'))
            throw new ArgumentException($"Invalid {label}: \"{id}\"", label);
        return id;
    }

    private static string FormatUserIdUrl(string userId) =>
        $"https://graph.microsoft.com/v1.0/users('{Uri.EscapeDataString(userId)}')";
}

/// <summary>Payload for sending a message to a Teams chat via Microsoft Graph.</summary>
internal sealed record TeamsChatMessagePayload
{
    [JsonPropertyName("body")]
    public required TeamsMessageBody Body { get; init; }
}

/// <summary>The body of a Teams message.</summary>
internal sealed record TeamsMessageBody
{
    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

/// <summary>
/// Persisted OAuth tokens for Teams authentication.
/// </summary>
internal sealed record StoredTokens
{
    /// <summary>Gets the access token.</summary>
    public required string AccessToken { get; init; }
    /// <summary>Gets the refresh token.</summary>
    public required string RefreshToken { get; init; }
    /// <summary>Gets the expiry time (UTC).</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>Payload for creating a new Teams chat via Microsoft Graph.</summary>
internal sealed record TeamsChatPayload
{
    [JsonPropertyName("chatType")]
    public required string ChatType { get; init; }

    [JsonPropertyName("members")]
    public required TeamsConversationMember[] Members { get; init; }
}

/// <summary>A conversation member entry for a new Teams chat payload.</summary>
internal sealed record TeamsConversationMember
{
    [JsonPropertyName("@odata.type")]
    public required string OdataType { get; init; }

    [JsonPropertyName("roles")]
    public required string[] Roles { get; init; }

    [JsonPropertyName("user@odata.bind")]
    public required string UserId { get; init; }
}

/// <summary>Source-generated JSON context for Teams adapter types.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredTokens))]
[JsonSerializable(typeof(TeamsChatPayload))]
[JsonSerializable(typeof(TeamsConversationMember))]
[JsonSerializable(typeof(TeamsChatMessagePayload))]
[JsonSerializable(typeof(TeamsMessageBody))]
internal partial class TeamsJsonContext : JsonSerializerContext
{
}
