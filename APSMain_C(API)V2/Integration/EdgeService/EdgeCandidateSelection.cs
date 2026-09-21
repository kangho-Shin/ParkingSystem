namespace APSMain.Integration.EdgeService;

public static class EdgeCandidateSelection
{
    public static ParkingSearchCandidate? Find(
        IReadOnlyList<ParkingSearchCandidate> candidates, long parkingSessionId) =>
        candidates.FirstOrDefault(x => x.ParkingSessionId == parkingSessionId);
}
