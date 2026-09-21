using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Parking.Contracts;

string url = ReadOption(args, "--url") ?? "http://localhost:5200";
if (!long.TryParse(ReadOption(args, "--site"), out long sitenum) || sitenum <= 0 ||
    !int.TryParse(ReadOption(args, "--group"), out int groupnum) || groupnum <= 0 ||
    !int.TryParse(ReadOption(args, "--device-number"), out int devicenum) || devicenum <= 0)
{
    Console.Error.WriteLine("사용법: --site <현장번호> --group <그룹번호> --device-number <장비번호> [--url http://localhost:5200] [--complete]");
    return 1;
}
bool complete = args.Contains("--complete", StringComparer.OrdinalIgnoreCase);
using HttpClient http = new() { BaseAddress = new Uri(url.TrimEnd('/') + "/") };
await using HubConnection connection = new HubConnectionBuilder()
    .WithUrl(new Uri(http.BaseAddress, "hubs/kiosk"))
    .WithAutomaticReconnect()
    .Build();

connection.On<KioskExitNotification>("ExitVehicleDetected", async notification =>
{
    Console.WriteLine(JsonSerializer.Serialize(notification, new JsonSerializerOptions { WriteIndented = true }));
    if (!complete) return;
    string requestJson = JsonSerializer.Serialize(new KioskDeviceIdentity(sitenum, groupnum, devicenum));
    using HttpResponseMessage response = await http.PostAsync(
        $"api/v1/local/kiosks/events/{notification.EventId}/complete",
        new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json"));
    Console.WriteLine($"완료 전송: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
});
connection.Reconnected += async _ => await connection.InvokeAsync(
    "Register", sitenum, groupnum, devicenum);

using CancellationTokenSource shutdown = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};
await connection.StartAsync(shutdown.Token);
await connection.InvokeAsync("Register", sitenum, groupnum, devicenum, shutdown.Token);
Console.WriteLine($"무인정산기 {sitenum}/{groupnum}/{devicenum} 연결 완료. 종료하려면 Ctrl+C를 누르세요.");
try { await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token); }
catch (OperationCanceledException) { }
return 0;

static string? ReadOption(string[] arguments, string name)
{
    int index = Array.FindIndex(arguments, value =>
        string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}
