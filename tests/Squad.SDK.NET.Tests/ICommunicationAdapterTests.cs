using Squad.SDK.NET.Platform;

namespace Squad.SDK.NET.Tests;

public sealed class ICommunicationAdapterTests
{
    [Fact]
    public async Task LogoutAsync_DefaultImplementation_CompletesSuccessfully()
    {
        ICommunicationAdapter adapter = new NoOpCommunicationAdapter();

        var exception = await Record.ExceptionAsync(() => adapter.LogoutAsync());

        Assert.Null(exception);
    }

    private sealed class NoOpCommunicationAdapter : ICommunicationAdapter
    {
        public CommunicationChannel Channel => CommunicationChannel.FileLog;

        public Task<PostUpdateResult> PostUpdateAsync(PostUpdateOptions options, CancellationToken cancellationToken = default)
            => Task.FromResult(new PostUpdateResult { Id = "noop-thread", Url = "noop://noop-thread" });

        public Task<IReadOnlyList<CommunicationReply>> PollForRepliesAsync(
            string threadId,
            DateTimeOffset since,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommunicationReply>>(Array.Empty<CommunicationReply>());

        public string? GetNotificationUrl(string threadId) => $"noop://{threadId}";
    }
}
