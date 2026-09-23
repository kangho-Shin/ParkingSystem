using JPXLpr.Edge;

namespace JPXLpr.Tests;

public sealed class EdgeLprOutboxTests
{
    [Fact]
    public async Task Enqueue_is_persisted_and_accepted_item_is_removed()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "outbox.json");
        AcceptingSender sender = new();
        await using EdgeLprOutbox outbox = new(path, sender, TimeSpan.FromMilliseconds(10));
        EdgeLprEvent value = TestEvent(401);

        outbox.Enqueue(value);
        Assert.True(File.Exists(path));
        outbox.Start();
        await sender.Sent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => outbox.Snapshot().Count == 0);

        Assert.Empty(outbox.Snapshot());
    }

    [Fact]
    public async Task Saved_event_is_loaded_after_restart()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "outbox.json");
        await using (EdgeLprOutbox first = new(path, new RejectingSender())) first.Enqueue(TestEvent(401));
        await using EdgeLprOutbox second = new(path, new RejectingSender());
        Assert.Single(second.Snapshot());
    }

    [Fact]
    public async Task Four_camera_events_keep_distinct_identity_and_event_ids()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "outbox.json");
        await using EdgeLprOutbox outbox = new(path, new RejectingSender());
        for (int camera = 1; camera <= 4; camera++) outbox.Enqueue(TestEvent(400 + camera));
        Assert.Equal(4, outbox.Snapshot().Select(x => x.Event.EventId).Distinct().Count());
        Assert.Equal(4, outbox.Snapshot().Select(x => x.Event.FileName).Distinct().Count());
    }

    [Fact]
    public async Task Rejected_camera_does_not_block_another_camera()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "outbox.json");
        SelectiveSender sender = new();
        await using EdgeLprOutbox outbox = new(path, sender, TimeSpan.FromSeconds(10));
        EdgeLprEvent rejected = TestEvent(401);
        EdgeLprEvent accepted = TestEvent(402);
        outbox.Enqueue(rejected);
        outbox.Enqueue(accepted);

        outbox.Start();
        await sender.Accepted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => outbox.Snapshot().All(x => x.Event.EventId != accepted.EventId));

        Assert.Contains(outbox.Snapshot(), x => x.Event.EventId == rejected.EventId);
        Assert.DoesNotContain(outbox.Snapshot(), x => x.Event.EventId == accepted.EventId);
    }

    [Fact]
    public async Task Permanent_nak_is_removed_without_retry()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "outbox.json");
        PermanentNakSender sender = new();
        await using EdgeLprOutbox outbox = new(path, sender, TimeSpan.FromMilliseconds(10));
        outbox.Enqueue(TestEvent(401));

        outbox.Start();
        await sender.Sent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => outbox.Snapshot().Count == 0);

        Assert.Empty(outbox.Snapshot());
        Assert.Equal(1, sender.SendCount);
    }

    [Fact]
    public async Task Payment_required_is_removed_without_endless_retry()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "outbox.json");
        PaymentRequiredSender sender = new();
        await using EdgeLprOutbox outbox = new(path, sender, TimeSpan.FromMilliseconds(10));
        outbox.Enqueue(TestEvent(402));

        outbox.Start();
        await sender.Sent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => outbox.Snapshot().Count == 0);

        Assert.Empty(outbox.Snapshot());
        Assert.Equal(1, sender.SendCount);
    }

    private static EdgeLprEvent TestEvent(int device) => EdgeLprEvent.Create(
        9001, 2, device, 9010, "Entry", DateTime.Now, $"12가{device:0000}", Guid.NewGuid());

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
    }

    private sealed class AcceptingSender : IEdgeLprSender
    {
        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
        {
            Sent.TrySetResult();
            return Task.FromResult(new EdgeLprSendResult(true, "ACK", value.EventId.ToString()));
        }
    }

    private sealed class RejectingSender : IEdgeLprSender
    {
        public Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token) =>
            Task.FromResult(new EdgeLprSendResult(false, "NACK", "test"));
    }

    private sealed class SelectiveSender : IEdgeLprSender
    {
        public TaskCompletionSource Accepted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
        {
            bool accepted = value.Devicenum == 402;
            if (accepted) Accepted.TrySetResult();
            return Task.FromResult(new EdgeLprSendResult(accepted, accepted ? "ACK" : "NAK", "test"));
        }
    }

    private sealed class PermanentNakSender : IEdgeLprSender
    {
        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int SendCount { get; private set; }

        public Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
        {
            SendCount++;
            Sent.TrySetResult();
            return Task.FromResult(new EdgeLprSendResult(
                false,
                "NAK",
                $"NAK|{value.EventId:N}|INVALID_DEVICE",
                "INVALID_DEVICE"));
        }
    }

    private sealed class PaymentRequiredSender : IEdgeLprSender
    {
        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int SendCount { get; private set; }

        public Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
        {
            SendCount++;
            Sent.TrySetResult();
            return Task.FromResult(new EdgeLprSendResult(
                false,
                "NAK",
                $"NAK|{value.EventId:N}|PAYMENT_REQUIRED",
                "PAYMENT_REQUIRED"));
        }
    }
}
