namespace Parking.EdgeManager.Core;

public sealed class ParkingCentralUnavailableException : Exception
{
    public ParkingCentralUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
