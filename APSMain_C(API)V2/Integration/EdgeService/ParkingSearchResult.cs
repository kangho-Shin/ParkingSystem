namespace APSMain.Integration.EdgeService;

public sealed record ParkingSearchResult(
    IReadOnlyList<ParkingSearchCandidate> Candidates,
    FeeQuote? Quote);
