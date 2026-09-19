namespace Parking.Contracts;

public sealed record HttpRelayResponse(
    int StatusCode,
    string Content);
