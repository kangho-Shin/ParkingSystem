using System.Globalization;

namespace Parking.LprStressTester;

public sealed record LprStressOptions(
    string Host,
    int Port,
    long SiteId,
    int Groupnum,
    long LaneId,
    IReadOnlyList<int> DeviceNumbers,
    int Count,
    int IntervalMilliseconds,
    int TimeoutSeconds)
{
    public static LprStressOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("모든 인수는 --이름 값 형식이어야 합니다.");
            values[args[index]] = args[index + 1];
        }

        string host = Get(values, "--host", "localhost").Trim();
        int port = ParseInt(values, "--port", 29200, 1, 65535);
        long siteId = ParseLong(values, "--site", 9001, 1, long.MaxValue);
        int groupnum = ParseInt(values, "--group", 2, 1, 999);
        long laneId = ParseLong(values, "--lane", 9010, 1, long.MaxValue);
        int count = ParseInt(values, "--count", 20, 1, 100000);
        int interval = ParseInt(values, "--interval-ms", 50, 0, 60000);
        int timeout = ParseInt(values, "--timeout-seconds", 10, 1, 3600);
        int[] devices = Get(values, "--devices", "411,412,413,414")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0)
            .ToArray();

        string[] supported = { "--host", "--port", "--site", "--group", "--lane", "--devices", "--count", "--interval-ms", "--timeout-seconds" };
        string? unknown = values.Keys.FirstOrDefault(key => !supported.Contains(key, StringComparer.OrdinalIgnoreCase));
        if (unknown is not null) throw new ArgumentException($"지원하지 않는 인수입니다: {unknown}");
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("호스트 주소가 비어 있습니다.");
        if (devices.Length is < 1 or > 4 || devices.Any(value => value is < 1 or > 999) || devices.Distinct().Count() != devices.Length)
            throw new ArgumentException("장비번호는 중복 없이 1~999 범위에서 1~4개를 지정해야 합니다.");

        return new(host, port, siteId, groupnum, laneId, devices, count, interval, timeout);
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string name, string defaultValue) =>
        values.TryGetValue(name, out string? value) ? value : defaultValue;

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string name, int defaultValue, int minimum, int maximum)
    {
        string text = Get(values, name, defaultValue.ToString(CultureInfo.InvariantCulture));
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < minimum || value > maximum)
            throw new ArgumentException($"{name} 값은 {minimum}~{maximum} 범위여야 합니다.");
        return value;
    }

    private static long ParseLong(IReadOnlyDictionary<string, string> values, string name, long defaultValue, long minimum, long maximum)
    {
        string text = Get(values, name, defaultValue.ToString(CultureInfo.InvariantCulture));
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long value) || value < minimum || value > maximum)
            throw new ArgumentException($"{name} 값이 올바르지 않습니다.");
        return value;
    }
}
