namespace Parking.Contracts;

public sealed record KioskDeviceIdentity(
    long Sitenum,
    int Groupnum,
    int Devicenum);
