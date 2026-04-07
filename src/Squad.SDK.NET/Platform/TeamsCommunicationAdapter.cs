using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Squad.SDK.NET.Platform;

/// <summary>
/// Configuration for the Microsoft Teams communication adapter.
/// </summary>
public sealed record TeamsCommsConfig
{
    /// <summary>Gets the Entra tenant ID. Defaults to <c>"organizations"</c> (multi-tenant).</summary>
    public string? TenantId { get; init; }
    /// <summary>Gets the Entra application (client) ID. Defaults to the Microsoft Graph PowerShell first-party app.</summary>
    public string? ClientId { get; init; }
    /// <summary>Gets the recipient UPN for 1:1 chat (e.g. <c>"user@contoso.com"</c>).</summary>
    public string? RecipientUpn { get; init; }
    /// <summary>Gets an existing chat ID to use directly (skips chat creation).</summary>
    public string? ChatId { get; init; }
    /// <summary>Gets the Teams channel ID for channel messages (requires <see cref="TeamId"/>).</summary>
    public string? ChannelId { get; init; }
    /// <summary>Gets the Teams team ID for channel messages.</summary>
    public string? TeamId { get; init; }
}

/// <summary>
/// Microsoft Teams communication adapter — bidirectional chat via the Microsoft Graph API.
///
/// Auth priority: cached token → token refresh → browser PKCE → device code fallback.
/// Uses the Microsoft Graph PowerShell first-party client ID by default, which works
/// in every Microsoft tenant without a custom Entra app registration.
/// Tokens are stored in <c>~/.squad/teams-tokens.json</c>.
/// </summary>
public sealed class TeamsCommunicationAdapter : ICommunicationAdapter
{
    /// <summary>Microsoft Graph PowerShell — first-party, present in every tenant.</summary>
    private const string DefaultClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
    /// <summary>Multi-tenant endpoint — works for any Entra organisation.</summary>
    private const string DefaultTenantId = "organizations";
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string Scopes = "Chat.ReadWrite ChatMessage.Send ChatMessage.Read User.Read offline_access";

    private static readonly string s_squadDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".squad");
    private static readonly string s_tokenPath = Path.Combine(s_squadDir, "teams-tokens.json");

    private readonly TeamsCommsConfig _config;
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly HttpClient _http;
    private StoredTokens? _tokens;
    private string? _resolvedChatId;
    private string? _cachedUserId;

    /// <summary>
    /// Initializes a new <see cref="TeamsCommunicationAdapter"/> with the given configuration.
    /// </summary>
    /// <param name="config">Teams adapter configuration.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> to use; a new instance is created when <see langword="null"/>.</param>
    public TeamsCommunicationAdapter(TeamsCommsConfig config, HttpClient? httpClient = null)
    {
        _config = config;
        _clientId = config.ClientId ?? DefaultClientId;
        _tenantId = config.TenantId ?? DefaultTenantId;
        _resolvedChatId = config.ChatId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public CommunicationChannel Channel => CommunicationChannel.TeamsGraph;

    /// <inheritdoc/>
    public async Task<PostUpdateResult> PostUpdateAsync(PostUpdateOptions options, CancellationToken cancellationToken = default)
    {
        var accessToken = await EnsureAuthenticatedAsync(cancellationToken);

        // Channel message mode: teamId + channelId configured
        if (!string.IsNullOrEmpty(_config.TeamId) && !string.IsNullOrEmpty(_config.ChannelId))
        {
            var safeTeamId = ValidateGraphId(_config.TeamId, "teamId");
            var safeChannelId = ValidateGraphId(_config.ChannelId, "channelId");
            var url = $"{GraphBase}/teams/{safeTeamId}/channels/{safeChannelId}/messages";
            await PostMessageAsync(url, accessToken, options, cancellationToken);

            return new PostUpdateResult
            {
                Id = $"{_config.TeamId}|{_config.ChannelId}",
                Url = $"https://teams.microsoft.com/l/channel/{Uri.EscapeDataString(_config.ChannelId)}"
            };
        }

        // 1:1 chat mode
        var chatId = await EnsureChatAsync(accessToken, cancellationToken);
        var safeChatId = ValidateGraphId(chatId, "chatId");
        var chatUrl = $"{GraphBase}/chats/{safeChatId}/messages";
        await PostMessageAsync(chatUrl, accessToken, options, cancellationToken);

        return new PostUpdateResult
        {
            Id = chatId,
            Url = GetNotificationUrl(chatId)
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CommunicationReply>> PollForRepliesAsync(
        string threadId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await EnsureAuthenticatedAsync(cancellationToken);
        var sinceIso = since.ToUniversalTime().ToString("o");

        string messagesUrl;
        if (threadId.Contains('|'))
        {
            var parts = threadId.Split('|', 2);
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                var safeTeamId = ValidateGraphId(parts[0], "teamId");
                var safeChannelId = ValidateGraphId(parts[1], "channelId");
                messagesUrl = $"{GraphBase}/teams/{safeTeamId}/channels/{safeChannelId}/messages" +
                              $"?$filter=createdDateTime+gt+{Uri.EscapeDataString(sinceIso)}&$top=50&$orderby=createdDateTime+asc";
            }
            else
            {
                return [];
            }
        }
        else
        {
            var chatId = _resolvedChatId ?? threadId;
            var safeChatId = ValidateGraphId(chatId, "chatId");
            messagesUrl = $"{GraphBase}/chats/{safeChatId}/messages" +
                          $"?$filter=createdDateTime+gt+{Uri.EscapeDataString(sinceIso)}&$top=50&$orderby=createdDateTime+asc";
        }

        GraphMessageList? data;
        try
        {
            data = await GraphFetchAsync<GraphMessageList>(messagesUrl, accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Teams pollForReplies failed: {ex.Message}");
            return [];
        }

        if (data?.Value is null)
            return [];

        var myId = await GetMyUserIdAsync(accessToken, cancellationToken);

        var replies = new List<CommunicationReply>();
        foreach (var m in data.Value)
        {
            if (m.From?.User is null) continue;
            if (myId is not null && m.From.User.Id == myId) continue;
            if (!DateTimeOffset.TryParse(m.CreatedDateTime, out var created)) continue;
            if (created <= since) continue;

            replies.Add(new CommunicationReply
            {
                Author = m.From.User.DisplayName ?? "unknown",
                Body = StripHtml(m.Body?.Content ?? string.Empty),
                Timestamp = created,
                Id = m.Id ?? string.Empty
            });
        }

        return replies.AsReadOnly();
    }

    /// <inheritdoc/>
    public string? GetNotificationUrl(string threadId)
    {
        var chatId = _resolvedChatId ?? threadId;
        return $"https://teams.microsoft.com/l/chat/{Uri.EscapeDataString(chatId)}";
    }

    // ─── Auth ─────────────────────────────────────────────────────────────

    /// <summary>Ensures a valid access token is available.</summary>
    private async Task<string> EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        _tokens ??= LoadTokens();

        // Valid token
        if (_tokens is not null && DateTimeOffset.UtcNow < _tokens.ExpiresAt.AddMinutes(-1))
            return _tokens.AccessToken;

        // Expired but have refresh token — try refresh
        if (_tokens?.RefreshToken is not null)
        {
            try
            {
                _tokens = await RefreshAccessTokenAsync(_tokens.RefreshToken, cancellationToken);
                return _tokens.AccessToken;
            }
            catch
            {
                Console.WriteLine("⚠️  Token refresh failed — re-authenticating...");
            }
        }

        // Try browser PKCE auth first
        try
        {
            _tokens = await StartBrowserAuthFlowAsync(cancellationToken);
            Console.WriteLine("✅ Teams authentication successful — tokens saved");
            return _tokens.AccessToken;
        }
        catch
        {
            Console.WriteLine("Browser auth unavailable, falling back to device code...");
        }

        // Fallback: device code flow (works in headless/SSH environments)
        _tokens = await StartDeviceCodeFlowAsync(cancellationToken);
        return _tokens.AccessToken;
    }

    /// <summary>Finds or creates a 1:1 chat with the configured recipient.</summary>
    private async Task<string> EnsureChatAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (_resolvedChatId is not null) return _resolvedChatId;

        var upn = _config.RecipientUpn;

        // "me" mode requires an explicit chat ID
        if (string.IsNullOrEmpty(upn) || string.Equals(upn, "me", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(_config.ChatId))
                throw new InvalidOperationException(
                    "Teams \"me\" mode requires an explicit chatId to avoid routing messages to the wrong 1:1 chat. " +
                    "Provide config.ChatId or set RecipientUpn to a specific user.");
            ValidateGraphId(_config.ChatId, "chatId");
            _resolvedChatId = _config.ChatId;
            return _resolvedChatId;
        }

        var safeUpn = ValidateGraphId(upn, "recipientUpn");
        var me = await GraphFetchAsync<GraphUser>($"{GraphBase}/me", accessToken, cancellationToken)
                 ?? throw new InvalidOperationException("Failed to retrieve current user from Graph.");

        var chatBody = new
        {
            chatType = "oneOnOne",
            members = new object[]
            {
                new
                {
                    odataType = "#microsoft.graph.aadUserConversationMember",
                    roles = new[] { "owner" },
                    userOdataBind = $"https://graph.microsoft.com/v1.0/users('{me.Id}')"
                },
                new
                {
                    odataType = "#microsoft.graph.aadUserConversationMember",
                    roles = new[] { "owner" },
                    userOdataBind = $"https://graph.microsoft.com/v1.0/users('{safeUpn}')"
                }
            }
        };

        using var createChatReq = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/chats");
        createChatReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        createChatReq.Content = new StringContent(
            JsonSerializer.Serialize(chatBody, TeamsJsonContext.Default.Object),
            Encoding.UTF8, "application/json");

        using var createChatResp = await _http.SendAsync(createChatReq, cancellationToken);
        var chatJson = await createChatResp.Content.ReadAsStringAsync(cancellationToken);
        var chatResult = JsonSerializer.Deserialize(chatJson, TeamsJsonContext.Default.GraphChat);
        _resolvedChatId = chatResult?.Id ?? throw new InvalidOperationException("Chat creation returned no ID.");
        return _resolvedChatId;
    }

    // ─── Graph Helpers ────────────────────────────────────────────────────

    private async Task<T?> GraphFetchAsync<T>(string url, string accessToken, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await _http.SendAsync(req, cancellationToken);

            if (resp.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or (HttpStatusCode)504)
            {
                if (attempt < maxRetries)
                {
                    var delay = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            return (T?)JsonSerializer.Deserialize(json, typeof(T), TeamsJsonContext.Default);
        }

        throw new HttpRequestException("Graph API request failed after retries.");
    }

    private async Task PostMessageAsync(string url, string accessToken, PostUpdateOptions options, CancellationToken cancellationToken)
    {
        var body = new GraphMessageBody
        {
            Body = new GraphMessageContent
            {
                ContentType = "html",
                Content = FormatTeamsMessage(options.Title, options.Body, options.Author)
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = new StringContent(
            JsonSerializer.Serialize(body, TeamsJsonContext.Default.GraphMessageBody),
            Encoding.UTF8, "application/json");

        const int maxRetries = 3;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var resp = await _http.SendAsync(req, cancellationToken);

            if (resp.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or (HttpStatusCode)504)
            {
                if (attempt < maxRetries)
                {
                    var delay = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }

            resp.EnsureSuccessStatusCode();
            return;
        }
    }

    private async Task<string?> GetMyUserIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (_cachedUserId is not null) return _cachedUserId;
        try
        {
            var me = await GraphFetchAsync<GraphUser>($"{GraphBase}/me", accessToken, cancellationToken);
            _cachedUserId = me?.Id;
            return _cachedUserId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Teams /me fetch failed: {ex.Message}");
            return null;
        }
    }

    // ─── Browser PKCE Flow ────────────────────────────────────────────────

    private async Task<StoredTokens> StartBrowserAuthFlowAsync(CancellationToken cancellationToken)
    {
        var codeVerifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var codeChallenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var oauthState = Base64Url(RandomNumberGenerator.GetBytes(16));

        var tcs = new TaskCompletionSource<StoredTokens>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:0/");

        // Find a free port
        int port;
        using (var socket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0))
        {
            socket.Start();
            port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
        }

        listener.Prefixes.Clear();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        var redirectUri = $"http://localhost:{port}/";
        var authUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(_tenantId)}/oauth2/v2.0/authorize" +
                      $"?client_id={Uri.EscapeDataString(_clientId)}" +
                      $"&response_type=code" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(Scopes)}" +
                      $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                      $"&code_challenge_method=S256" +
                      $"&state={Uri.EscapeDataString(oauthState)}" +
                      $"&prompt=select_account";

        Console.WriteLine("🌐 Opening browser for Teams authentication...");
        OpenBrowser(authUrl);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(120));

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var contextTask = listener.GetContextAsync();
                await Task.WhenAny(contextTask, Task.Delay(-1, cts.Token));

                if (cts.Token.IsCancellationRequested)
                    break;

                var context = await contextTask;
                var reqUrl = context.Request.Url;
                if (reqUrl is null) { context.Response.Close(); continue; }

                var query = System.Web.HttpUtility.ParseQueryString(reqUrl.Query);
                var code = query["code"];
                var error = query["error"];
                var returnedState = query["state"];

                if (error is not null)
                {
                    var html = $"<html><body><h1>Authentication failed</h1><p>{System.Web.HttpUtility.HtmlEncode(error)}</p></body></html>";
                    await WriteResponseAsync(context.Response, html, "text/html");
                    throw new InvalidOperationException($"Browser auth denied: {error}");
                }

                if (returnedState != oauthState)
                {
                    await WriteResponseAsync(context.Response, "Invalid state — possible CSRF", "text/plain");
                    throw new InvalidOperationException("OAuth state mismatch — possible CSRF");
                }

                if (code is null)
                {
                    await WriteResponseAsync(context.Response, "Missing authorization code", "text/plain");
                    continue;
                }

                // Exchange code for tokens
                var tokens = await ExchangeCodeForTokensAsync(code, redirectUri, codeVerifier, cancellationToken);
                SaveTokens(tokens);

                const string successHtml = "<html><body><h1>✅ Authentication successful!</h1><p>You can close this tab.</p></body></html>";
                await WriteResponseAsync(context.Response, successHtml, "text/html");
                return tokens;
            }
        }
        finally
        {
            listener.Stop();
        }

        throw new OperationCanceledException("Browser auth timed out or was cancelled.");
    }

    private async Task<StoredTokens> ExchangeCodeForTokensAsync(
        string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken)
    {
        var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(_tenantId)}/oauth2/v2.0/token";
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _clientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
            ["scope"] = Scopes
        });

        using var resp = await _http.PostAsync(tokenUrl, body, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var tokenResp = JsonSerializer.Deserialize(json, TeamsJsonContext.Default.TokenResponse)
                        ?? throw new InvalidOperationException("Token exchange returned empty response.");

        if (string.IsNullOrEmpty(tokenResp.AccessToken))
            throw new InvalidOperationException($"Token exchange failed: {tokenResp.Error} — {tokenResp.ErrorDescription}");

        return ParseTokens(tokenResp);
    }

    // ─── Device Code Flow ─────────────────────────────────────────────────

    private async Task<StoredTokens> StartDeviceCodeFlowAsync(CancellationToken cancellationToken)
    {
        var deviceCodeUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(_tenantId)}/oauth2/v2.0/devicecode";
        var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(_tenantId)}/oauth2/v2.0/token";

        var dcBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["scope"] = Scopes
        });

        using var dcResp = await _http.PostAsync(deviceCodeUrl, dcBody, cancellationToken);
        if (!dcResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Device code request failed: {dcResp.StatusCode} {await dcResp.Content.ReadAsStringAsync(cancellationToken)}");

        var dcJson = await dcResp.Content.ReadAsStringAsync(cancellationToken);
        var dcData = JsonSerializer.Deserialize(dcJson, TeamsJsonContext.Default.DeviceCodeResponse)
                     ?? throw new InvalidOperationException("Device code response was empty.");

        Console.WriteLine($"\n🔐 Teams authentication required");
        Console.WriteLine($"   {dcData.Message}\n");

        var pollInterval = TimeSpan.FromSeconds(dcData.Interval > 0 ? dcData.Interval : 5);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(dcData.ExpiresIn);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(pollInterval, cancellationToken);

            var pollBody = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["client_id"] = _clientId,
                ["device_code"] = dcData.DeviceCode
            });

            using var tokenResp = await _http.PostAsync(tokenUrl, pollBody, cancellationToken);
            var tokenJson = await tokenResp.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize(tokenJson, TeamsJsonContext.Default.TokenResponse);

            if (!string.IsNullOrEmpty(tokenData?.AccessToken))
            {
                var tokens = ParseTokens(tokenData);
                SaveTokens(tokens);
                Console.WriteLine("✅ Teams authentication successful — tokens saved\n");
                return tokens;
            }

            if (tokenData?.Error == "authorization_pending") continue;
            if (tokenData?.Error == "slow_down")
            {
                pollInterval = pollInterval.Add(TimeSpan.FromSeconds(5));
                continue;
            }

            throw new InvalidOperationException($"Device code auth failed: {tokenData?.Error} — {tokenData?.ErrorDescription}");
        }

        throw new OperationCanceledException("Device code flow timed out — user did not authenticate in time.");
    }

    // ─── Token Refresh ────────────────────────────────────────────────────

    private async Task<StoredTokens> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(_tenantId)}/oauth2/v2.0/token";
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _clientId,
            ["refresh_token"] = refreshToken,
            ["scope"] = Scopes
        });

        using var resp = await _http.PostAsync(tokenUrl, body, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var tokenResp = JsonSerializer.Deserialize(json, TeamsJsonContext.Default.TokenResponse)
                        ?? throw new InvalidOperationException("Token refresh returned empty response.");

        if (string.IsNullOrEmpty(tokenResp.AccessToken))
            throw new InvalidOperationException($"Token refresh failed: {tokenResp.Error} — {tokenResp.ErrorDescription}");

        var tokens = ParseTokens(tokenResp);
        SaveTokens(tokens);
        return tokens;
    }

    // ─── Token Storage ────────────────────────────────────────────────────

    private static StoredTokens? LoadTokens()
    {
        try
        {
            if (!File.Exists(s_tokenPath)) return null;
            var raw = File.ReadAllText(s_tokenPath);
            return JsonSerializer.Deserialize(raw, TeamsJsonContext.Default.StoredTokens);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveTokens(StoredTokens tokens)
    {
        if (!Directory.Exists(s_squadDir))
            Directory.CreateDirectory(s_squadDir);

        var json = JsonSerializer.Serialize(tokens, TeamsJsonContext.Default.StoredTokens);
        File.WriteAllText(s_tokenPath, json);

        // Set restrictive permissions on non-Windows platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // chmod 600 for tokens, 700 for directory
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    ArgumentList = { "600", s_tokenPath },
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    ArgumentList = { "700", s_squadDir },
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();
            }
            catch { /* chmod failure is non-fatal */ }
        }
    }

    private static StoredTokens ParseTokens(TokenResponse resp) =>
        new StoredTokens
        {
            AccessToken = resp.AccessToken!,
            RefreshToken = resp.RefreshToken ?? string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(resp.ExpiresIn)
        };

    // ─── Formatting ───────────────────────────────────────────────────────

    internal static string FormatTeamsMessage(string title, string body, string? author)
    {
        var authorLine = author is not null ? $"<em>Posted by {EscapeHtml(author)}</em><br/>" : string.Empty;
        return $"<b>{EscapeHtml(title)}</b><br/>{authorLine}<br/>{EscapeHtml(body).Replace("\n", "<br/>", StringComparison.Ordinal)}";
    }

    internal static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal)
         .Replace("\"", "&quot;", StringComparison.Ordinal)
         .Replace("'", "&#39;", StringComparison.Ordinal);

    internal static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Trim();

    internal static string ValidateGraphId(string id, string label)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[\w:@.\-]+$"))
            throw new ArgumentException($"Invalid {label}: contains unsafe characters.", label);
        return Uri.EscapeDataString(id);
    }

    // ─── Utility ──────────────────────────────────────────────────────────

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                ArgumentList = { "-NoProfile", "-Command", $"Start-Process '{url.Replace("'", "''")}'" },
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            System.Diagnostics.Process.Start("open", url);
        }
        else
        {
            System.Diagnostics.Process.Start("xdg-open", url);
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string body, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}

// ─── JSON Types ───────────────────────────────────────────────────────────

internal sealed class StoredTokens
{
    [JsonPropertyName("accessToken")] public required string AccessToken { get; set; }
    [JsonPropertyName("refreshToken")] public required string RefreshToken { get; set; }
    [JsonPropertyName("expiresAt")] public DateTimeOffset ExpiresAt { get; set; }
}

internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
}

internal sealed class DeviceCodeResponse
{
    [JsonPropertyName("device_code")] public required string DeviceCode { get; set; }
    [JsonPropertyName("user_code")] public required string UserCode { get; set; }
    [JsonPropertyName("verification_uri")] public required string VerificationUri { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
    [JsonPropertyName("message")] public required string Message { get; set; }
}

internal sealed class GraphUser
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
}

internal sealed class GraphChat
{
    [JsonPropertyName("id")] public string? Id { get; set; }
}

internal sealed class GraphMessageBody
{
    [JsonPropertyName("body")] public GraphMessageContent? Body { get; set; }
}

internal sealed class GraphMessageContent
{
    [JsonPropertyName("contentType")] public string? ContentType { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
}

internal sealed class GraphMessageList
{
    [JsonPropertyName("value")] public GraphMessage[]? Value { get; set; }
}

internal sealed class GraphMessage
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("createdDateTime")] public string? CreatedDateTime { get; set; }
    [JsonPropertyName("body")] public GraphMessageContent? Body { get; set; }
    [JsonPropertyName("from")] public GraphMessageFrom? From { get; set; }
}

internal sealed class GraphMessageFrom
{
    [JsonPropertyName("user")] public GraphUser? User { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredTokens))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(DeviceCodeResponse))]
[JsonSerializable(typeof(GraphUser))]
[JsonSerializable(typeof(GraphChat))]
[JsonSerializable(typeof(GraphMessageBody))]
[JsonSerializable(typeof(GraphMessageList))]
[JsonSerializable(typeof(GraphMessage))]
[JsonSerializable(typeof(object))]
internal partial class TeamsJsonContext : JsonSerializerContext
{
}
