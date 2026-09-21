namespace Parking.Worker;

public sealed class ParkingApiOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5000/";
    public int CheckIntervalSeconds { get; init; } = 30;
}
