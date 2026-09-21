namespace JPXLpr.Edge;

public sealed class EdgeLprOutboxItem
{
    public required EdgeLprEvent Event { get; init; }
    public int RetryCount { get; set; }
    public string LastError { get; set; } = string.Empty;
}
