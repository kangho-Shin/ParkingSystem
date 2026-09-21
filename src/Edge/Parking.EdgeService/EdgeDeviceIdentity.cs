namespace Parking.EdgeService;

public sealed record EdgeDeviceIdentity(
    long Sitenum,
    int Groupnum,
    int Devicenum,
    string DeviceType);

public sealed class DeviceIdentityException : Exception
{
    public DeviceIdentityException(string code) : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}
