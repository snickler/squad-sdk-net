using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET;

/// <summary>
/// Wraps a <see cref="CopilotSession"/> and exposes it through the <see cref="ISquadSession"/> interface.
/// </summary>
public sealed class SquadSession : ISquadSession
{
    private readonly CopilotSession _session;
    private readonly ILogger<SquadSession> _logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _toolCallNames = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SquadSession"/> class.
    /// </summary>
    /// <param name="session">The underlying <see cref="CopilotSession"/> to wrap.</param>
    /// <param name="logger">The logger for this session.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="session"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public SquadSession(CopilotSession session, ILogger<SquadSession> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string SessionId => _session.SessionId;

    /// <inheritdoc />
    public string? WorkspacePath => _session.WorkspacePath;

    /// <inheritdoc />
    public Task<string> SendAsync(SquadMessageOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to session {SessionId}", SessionId);
        var sdkOptions = MapMessageOptions(options);
        return _session.SendAsync(sdkOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> SendAndWaitAsync(SquadMessageOptions options, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to session {SessionId} and waiting for response", SessionId);
        var sdkOptions = MapMessageOptions(options);
        var response = await _session.SendAndWaitAsync(sdkOptions, timeout, cancellationToken);
        _logger.LogDebug("Received response from session {SessionId} ({Length} chars)", SessionId, response?.Data?.Content?.Length ?? 0);
        return response?.Data?.Content;
    }

    /// <inheritdoc />
    public Task AbortAsync(CancellationToken cancellationToken = default)
        => _session.AbortAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SquadEvent>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        var events = await _session.GetMessagesAsync(cancellationToken);
        return events.Select(MapSessionEvent).ToList();
    }

    /// <inheritdoc />
    public IDisposable On(Action<SquadEvent> handler)
    {
        return _session.On(evt => handler(MapSessionEvent(evt)));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _session.DisposeAsync();

    private static MessageOptions MapMessageOptions(SquadMessageOptions options)
    {
        var sdkOptions = new MessageOptions { Prompt = options.Prompt };

        if (options.Attachments is { Count: > 0 })
        {
            sdkOptions.Attachments = options.Attachments
                .Select(MapAttachment)
                .ToList();
        }

        return sdkOptions;
    }

    private static UserMessageAttachment MapAttachment(SquadAttachment attachment)
    {
        if (attachment.Data is not null)
        {
            return new UserMessageAttachmentBlob
            {
                Data        = attachment.Data,
                MimeType    = attachment.MimeType ?? string.Empty,
                DisplayName = attachment.DisplayName ?? string.Empty
            };
        }

        return new UserMessageAttachmentFile
        {
            Path        = attachment.Path ?? string.Empty,
            DisplayName = attachment.DisplayName ?? string.Empty
        };
    }

    private SquadEvent MapSessionEvent(SessionEvent evt)
    {
        var (type, payload) = evt switch
        {
            AssistantMessageEvent e => (
                SquadEventType.SessionMessage,
                (object?)e.Data?.Content),

            AssistantMessageDeltaEvent e => (
                SquadEventType.MessageDelta,
                e.Data is not null
                    ? new StreamDeltaPayload { Content = e.Data.DeltaContent ?? string.Empty }
                    : null),

            SessionIdleEvent => (
                SquadEventType.SessionIdle,
                (object?)null),

            SessionErrorEvent e => (
                SquadEventType.SessionError,
                e.Data is not null
                    ? new SessionErrorPayload { Message = e.Data.Message ?? e.Data.ErrorType ?? "Unknown error" }
                    : null),

            AssistantUsageEvent e => (
                SquadEventType.Usage,
                e.Data is not null
                    ? new UsagePayload
                    {
                        Model         = e.Data.Model ?? string.Empty,
                        InputTokens   = (int)(e.Data.InputTokens ?? 0),
                        OutputTokens  = (int)(e.Data.OutputTokens ?? 0),
                        EstimatedCost = (decimal)(e.Data.Cost ?? 0)
                    }
                    : null),

            AssistantReasoningDeltaEvent e => (
                SquadEventType.ReasoningDelta,
                e.Data is not null
                    ? new ReasoningDeltaPayload { Content = e.Data.DeltaContent ?? string.Empty }
                    : null),

            ToolExecutionStartEvent e => (
                SquadEventType.SessionToolCall,
                e.Data is not null
                    ? MapToolStart(e.Data)
                    : null),

            ToolExecutionCompleteEvent e => (
                SquadEventType.SessionToolCall,
                e.Data is not null
                    ? MapToolComplete(e.Data)
                    : null),

            _ => (SquadEventType.SessionMessage, (object?)null)
        };

        // Log usage and error events
        if (type == SquadEventType.Usage && payload is UsagePayload usage)
        {
            _logger.LogDebug("Usage: {Model} in={Input} out={Output}", usage.Model, usage.InputTokens, usage.OutputTokens);
        }
        else if (type == SquadEventType.SessionError && payload is SessionErrorPayload error)
        {
            _logger.LogWarning("Session error: {Message}", error.Message);
        }

        return new SquadEvent
        {
            Type      = type,
            SessionId = SessionId,
            Payload   = payload,
            Timestamp = evt.Timestamp
        };
    }

    private ToolCallPayload MapToolStart(ToolExecutionStartData data)
    {
        if (data.ToolCallId is not null && data.ToolName is not null)
            _toolCallNames[data.ToolCallId] = data.ToolName;

        return new ToolCallPayload
        {
            ToolName  = data.ToolName ?? string.Empty,
            Arguments = data.Arguments as IReadOnlyDictionary<string, object?>,
            Status    = ToolCallStatus.Running
        };
    }

    private ToolCallPayload MapToolComplete(ToolExecutionCompleteData data)
    {
        var toolName = data.ToolCallId is not null
            && _toolCallNames.TryRemove(data.ToolCallId, out var name)
            ? name
            : data.ToolCallId ?? string.Empty;

        return new ToolCallPayload
        {
            ToolName = toolName,
            Status   = data.Error is null ? ToolCallStatus.Completed : ToolCallStatus.Error
        };
    }
}
