using Newtonsoft.Json;

namespace JPXLpr.Edge;

public sealed class EdgeLprOutbox : IAsyncDisposable
{
    private readonly string _path;
    private readonly IEdgeLprSender _sender;
    private readonly TimeSpan _retryDelay;
    private readonly Action<string>? _log;
    private readonly List<EdgeLprOutboxItem> _items;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;

    public EdgeLprOutbox(string path, IEdgeLprSender sender, TimeSpan? retryDelay = null, Action<string>? log = null)
    {
        _path = path;
        _sender = sender;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
        _log = log;
        _items = Load(path);
    }

    public void Start()
    {
        if (_worker is not null) return;
        _worker = Task.Run(() => WorkAsync(_stop.Token));
        if (Snapshot().Count > 0) _signal.Release();
    }

    public void Enqueue(EdgeLprEvent value)
    {
        lock (_sync)
        {
            if (_items.Any(x => x.Event.EventId == value.EventId)) return;
            _items.Add(new EdgeLprOutboxItem { Event = value });
            SaveLocked();
        }
        if (_worker is not null) _signal.Release();
    }

    public IReadOnlyList<EdgeLprOutboxItem> Snapshot()
    {
        lock (_sync) return _items.Select(x => new EdgeLprOutboxItem
        {
            Event = x.Event, RetryCount = x.RetryCount, LastError = x.LastError
        }).ToArray();
    }

    private async Task WorkAsync(CancellationToken token)
    {
        int consecutiveFailures = 0;
        while (!token.IsCancellationRequested)
        {
            await _signal.WaitAsync(token);
            while (!token.IsCancellationRequested)
            {
                EdgeLprOutboxItem? item;
                lock (_sync) item = _items.FirstOrDefault();
                if (item is null) break;
                EdgeLprSendResult result = await _sender.SendAsync(item.Event, token);
                bool permanentFailure = EdgeLprResponseParser.IsPermanentFailure(result);
                lock (_sync)
                {
                    EdgeLprOutboxItem? current = _items.FirstOrDefault(x => x.Event.EventId == item.Event.EventId);
                    if (current is null) continue;
                    if (result.Accepted || permanentFailure)
                    {
                        _items.Remove(current);
                        consecutiveFailures = 0;
                    }
                    else
                    {
                        current.RetryCount++;
                        current.LastError = $"{result.Code}: {result.Message}";
                        _items.Remove(current);
                        _items.Add(current);
                        consecutiveFailures++;
                    }
                    SaveLocked();
                }
                TryLog(result.Accepted
                    ? $"EdgeService ACK: {item.Event.EventId:N}"
                    : permanentFailure
                        ? $"EdgeService {result.Code}: {item.Event.EventId:N}, discarded, {result.Message}"
                        : $"EdgeService {result.Code}: {item.Event.EventId:N}, retry={item.RetryCount}, {result.Message}");
                int pendingCount;
                lock (_sync) pendingCount = _items.Count;
                if (!result.Accepted && !permanentFailure && consecutiveFailures >= Math.Max(1, pendingCount))
                {
                    consecutiveFailures = 0;
                    await Task.Delay(_retryDelay, token);
                }
            }
        }
    }

    private static List<EdgeLprOutboxItem> Load(string path)
    {
        if (!File.Exists(path)) return new();
        try { return JsonConvert.DeserializeObject<List<EdgeLprOutboxItem>>(File.ReadAllText(path)) ?? new(); }
        catch
        {
            File.Move(path, path + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true);
            return new();
        }
    }

    private void SaveLocked()
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonConvert.SerializeObject(_items, Formatting.Indented));
        File.Move(temp, _path, true);
    }

    private void TryLog(string message)
    {
        try { _log?.Invoke(message); }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _signal.Release();
        if (_worker is not null) try { await _worker.ConfigureAwait(false); } catch (OperationCanceledException) { }
        _signal.Dispose();
        _stop.Dispose();
    }
}
