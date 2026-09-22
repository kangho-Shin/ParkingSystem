namespace Parking.Contracts;

public sealed record KioskDisplayRequest(
    KioskDeviceIdentity Device,
    string CarNumber,
    string DisplayMessage,
    int DisplaySeconds = 11);
