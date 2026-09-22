namespace Parking.Contracts;

public sealed record KioskManualExitRequest(
    KioskDeviceIdentity Device,
    string CarNumber,
    DateTimeOffset ExitAt);
