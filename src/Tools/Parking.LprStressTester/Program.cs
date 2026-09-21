using Parking.LprStressTester;

LprStressOptions options;
try
{
    options = LprStressOptions.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine($"인수 오류: {exception.Message}");
    PrintUsage();
    return 1;
}

using CancellationTokenSource cancellation = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine($"LPR 연속수신 시험 시작: {options.Host}:{options.Port}, 카메라={options.DeviceNumbers.Count}, 카메라별={options.Count}");
LprStressResult result = await new LprStressCoordinator().RunAsync(options, cancellation.Token);
foreach (LprCameraResult camera in result.Cameras.OrderBy(value => value.DeviceNumber))
{
    Console.WriteLine(
        $"장비 {camera.DeviceNumber:000}: 전송={camera.Sent}, ACK={camera.Acknowledged}, NAK={camera.Nak}, 시간초과={camera.Timeouts}, 프로토콜={camera.ProtocolErrors}, 연결={camera.ConnectionErrors}" +
        (camera.LastError is null ? "" : $", 마지막오류={camera.LastError}"));
}
Console.WriteLine($"합계: 전송={result.Sent}, ACK={result.Acknowledged}, NAK={result.Nak}, 시간초과={result.Timeouts}, 프로토콜={result.ProtocolErrors}, 연결={result.ConnectionErrors}");
Console.WriteLine(result.Success ? "시험 성공" : "시험 실패");
return LprStressExitCode.FromResult(result);

static void PrintUsage() => Console.WriteLine(
    "사용법: Parking.LprStressTester [--host localhost] [--port 29200] [--site 9001] [--group 2] [--lane 9010] [--devices 411,412,413,414] [--count 20] [--interval-ms 50] [--timeout-seconds 10]");
