namespace APSMain.Integration.EdgeService;

public sealed class KioskExitContext
{
    public required KioskExitNotification Notification { get; init; }
    public long? ParkingSessionId { get; set; }
    public FeeQuote? Quote { get; set; }
    public IReadOnlyList<ParkingSearchCandidate> Candidates { get; set; } = Array.Empty<ParkingSearchCandidate>();
    public bool IsEventDriven { get; init; }
}
