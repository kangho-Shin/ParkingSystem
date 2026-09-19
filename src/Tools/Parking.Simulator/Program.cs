using System.Text.Json;
using Parking.Contracts;
using Parking.Simulator;

if (args.Length == 0 ||
    (!string.Equals(args[0], "entry", StringComparison.OrdinalIgnoreCase) &&
     !string.Equals(args[0], "exit", StringComparison.OrdinalIgnoreCase)))
{
    PrintUsage();
    return 1;
}

if (!TryGetLong(args, "--site", out long siteId) ||
    !TryGetLong(args, "--lane", out long laneId) ||
    !TryGetLong(args, "--device", out long deviceId) ||
    !TryGetValue(args, "--car", out string carNumber))
{
    PrintUsage();
    return 1;
}

int groupnum = TryGetLong(args, "--group", out long configuredGroupnum)
    ? checked((int)configuredGroupnum)
    : 1;

string baseUrl = TryGetValue(args, "--url", out string configuredUrl)
    ? configuredUrl
    : "http://localhost:5200/";
Guid eventId = TryGetValue(args, "--event", out string eventText) &&
               Guid.TryParse(eventText, out Guid configuredEventId)
    ? configuredEventId
    : Guid.NewGuid();

using HttpClient httpClient = new() { BaseAddress = new Uri(baseUrl) };

try
{
    bool isEntry = string.Equals(args[0], "entry", StringComparison.OrdinalIgnoreCase);
    FieldEventResponse result = isEntry
        ? await new EntrySimulator(httpClient).SendAsync(
            eventId, siteId, laneId, deviceId, carNumber,
            CancellationToken.None, groupnum)
        : await new ExitSimulator(httpClient).SendAsync(
            eventId, siteId, groupnum, laneId, deviceId, carNumber,
            CancellationToken.None);
    Console.WriteLine(JsonSerializer.Serialize(result));

    if (args.Contains("--repeat-event", StringComparer.OrdinalIgnoreCase))
    {
        FieldEventResponse repeated = isEntry
            ? await new EntrySimulator(httpClient).SendAsync(
                eventId, siteId, laneId, deviceId, carNumber,
                CancellationToken.None, groupnum)
            : await new ExitSimulator(httpClient).SendAsync(
                eventId, siteId, groupnum, laneId, deviceId, carNumber,
                CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(repeated));
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"전송 실패: {exception.Message}");
    return 2;
}

static bool TryGetLong(string[] values, string name, out long value)
{
    value = 0;
    return TryGetValue(values, name, out string text) && long.TryParse(text, out value);
}

static bool TryGetValue(string[] values, string name, out string value)
{
    for (int index = 0; index < values.Length - 1; index++)
    {
        if (!string.Equals(values[index], name, StringComparison.OrdinalIgnoreCase))
            continue;

        value = values[index + 1];
        return true;
    }

    value = "";
    return false;
}

static void PrintUsage() => Console.WriteLine(
    "사용법: parking-simulator entry|exit --site 1 --group 1 --lane 10 --device 101 --car 12가3456 [--url http://localhost:5200] [--event UUID] [--repeat-event]");
