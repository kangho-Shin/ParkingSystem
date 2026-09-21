using System.Globalization;

namespace ParkImageServer;

public sealed record ImageFileName(string FileName, DateTime CaptureAt)
{
    public static bool TryParse(string? value, out ImageFileName? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value) ||
            Path.IsPathRooted(value) ||
            Path.GetFileName(value) != value ||
            value.Contains("..", StringComparison.Ordinal))
            return false;

        string extension = Path.GetExtension(value);
        if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            return false;

        string[] tokens = Path.GetFileNameWithoutExtension(value).Split('_');
        if (tokens.Length != 8 ||
            !long.TryParse(tokens[0], out long siteId) || siteId <= 0 ||
            !int.TryParse(tokens[1], out int groupnum) || groupnum <= 0 ||
            !int.TryParse(tokens[2], out int deviceNumber) || deviceNumber <= 0 ||
            !long.TryParse(tokens[3], out long laneId) || laneId <= 0 ||
            tokens[4] is not ("Entry" or "Exit") ||
            string.IsNullOrWhiteSpace(tokens[6]) ||
            !Guid.TryParse(tokens[7], out _))
            return false;

        if (!DateTime.TryParseExact(
                tokens[5],
                "yyyyMMddHHmmssfff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime captureAt))
            return false;

        result = new ImageFileName(value, captureAt);
        return true;
    }
}
