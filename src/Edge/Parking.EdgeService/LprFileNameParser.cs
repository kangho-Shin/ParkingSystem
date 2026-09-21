using System.Globalization;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class LprFileNameParser
{
    public LprParseResult Parse(string fileName)
    {
        Guid? extractedEventId = TryExtractEventId(fileName);
        if (string.IsNullOrWhiteSpace(fileName) ||
            Path.GetFileName(fileName) != fileName ||
            !string.Equals(Path.GetExtension(fileName), ".jpg", StringComparison.OrdinalIgnoreCase))
            return Failure(extractedEventId);

        string name = Path.GetFileNameWithoutExtension(fileName);
        string[] fields = name.Split('_');
        if (fields.Length != 8 ||
            !TryParsePositiveNumber(fields[0], out long siteId) ||
            !TryParseD3(fields[1], out long groupnum) ||
            !TryParseD3(fields[2], out long deviceId) ||
            !TryParsePositiveNumber(fields[3], out long laneId) ||
            groupnum > int.MaxValue)
            return Failure(extractedEventId);

        string direction = fields[4];
        if (direction != ParkingEventType.Entry && direction != ParkingEventType.Exit)
            return Failure(extractedEventId);

        if (!DateTime.TryParseExact(
                fields[5],
                "yyyyMMddHHmmssfff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime localTime))
            return Failure(extractedEventId);

        string carNumber = fields[6];
        if (string.IsNullOrWhiteSpace(carNumber) ||
            carNumber.Any(char.IsWhiteSpace))
            return Failure(extractedEventId);

        if (!Guid.TryParseExact(fields[7], "N", out Guid eventId))
            return Failure(extractedEventId);

        DateTime unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        DateTimeOffset recognizedAt = new(
            unspecified,
            TimeZoneInfo.Local.GetUtcOffset(unspecified));
        LprRecognition recognition = new(
            eventId,
            siteId,
            checked((int)groupnum),
            deviceId,
            laneId,
            direction,
            recognizedAt,
            carNumber,
            fileName);
        return new LprParseResult(true, recognition, null, eventId);
    }

    private static bool TryParseD3(string value, out long result)
    {
        result = 0;
        return value.Length == 3 &&
               value.All(character => character is >= '0' and <= '9') &&
               long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) &&
               result > 0;
    }

    private static bool TryParsePositiveNumber(string value, out long result)
    {
        result = 0;
        return value.Length > 0 &&
               value.All(character => character is >= '0' and <= '9') &&
               long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) &&
               result > 0;
    }

    private static Guid? TryExtractEventId(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        string candidate = Path.GetFileNameWithoutExtension(fileName).Split('_').LastOrDefault() ?? "";
        return Guid.TryParseExact(candidate, "N", out Guid eventId) ? eventId : null;
    }

    private static LprParseResult Failure(Guid? eventId) =>
        new(false, null, "INVALID_FILE_NAME", eventId);
}
