namespace Parking.LprStressTester;

public sealed record LprStressResult(IReadOnlyList<LprCameraResult> Cameras)
{
    public int Sent => Cameras.Sum(value => value.Sent);
    public int Acknowledged => Cameras.Sum(value => value.Acknowledged);
    public int Nak => Cameras.Sum(value => value.Nak);
    public int Timeouts => Cameras.Sum(value => value.Timeouts);
    public int ProtocolErrors => Cameras.Sum(value => value.ProtocolErrors);
    public int ConnectionErrors => Cameras.Sum(value => value.ConnectionErrors);
    public bool Success => Cameras.Count > 0 && Cameras.All(value => value.Success);
}

public sealed class LprStressCoordinator
{
    public async Task<LprStressResult> RunAsync(LprStressOptions options, CancellationToken cancellationToken)
    {
        Task<LprCameraResult>[] tasks = options.DeviceNumbers
            .Select(deviceNumber => new LprCameraRunner().RunAsync(options, deviceNumber, cancellationToken))
            .ToArray();
        LprCameraResult[] results = await Task.WhenAll(tasks);
        return new(results);
    }
}

public static class LprStressExitCode
{
    public static int FromResult(LprStressResult result) => result.Success ? 0 : 2;
}
