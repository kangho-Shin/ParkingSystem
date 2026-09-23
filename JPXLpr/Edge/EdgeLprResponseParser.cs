using System.Text;

namespace JPXLpr.Edge;

public static class EdgeLprResponseParser
{
    private static readonly HashSet<string> PermanentErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        "INVALID_FILE_NAME",
        "INVALID_SITE",
        "INVALID_LANE",
        "INVALID_DEVICE",
        "DEVICE_NOT_FOUND",
        "DIRECTION_MISMATCH",
        "PAYMENT_REQUIRED",
        "OPEN_SESSION_NOT_FOUND"
    };

    public static EdgeLprSendResult Match(string response, Guid eventId)
    {
        if (response.Length < 2 || response[0] != 0x02 || response[^1] != 0x03)
            return new(false, "INVALID_RESPONSE", response);

        string value = response[1..^1];
        string[] fields = value.Split('|');
        if (fields.Length == 3 &&
            (string.Equals(fields[0], "NAK", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(fields[0], "NACK", StringComparison.OrdinalIgnoreCase)))
        {
            if (!TryParseEventId(fields[1], out Guid actual) || !IsResultCode(fields[2]))
                return new(false, "INVALID_RESPONSE", value);
            if (actual != eventId)
                return new(false, "RESPONSE_EVENT_MISMATCH", value);
            return new(false, "NAK", value, fields[2]);
        }

        if (fields.Length != 2 ||
            !string.Equals(fields[0], "ACK", StringComparison.OrdinalIgnoreCase) ||
            !TryParseEventId(fields[1], out Guid ackEventId))
            return new(false, "INVALID_RESPONSE", value);
        if (ackEventId != eventId)
            return new(false, "RESPONSE_EVENT_MISMATCH", value);
        return new(true, "ACK", value);
    }

    public static bool IsPermanentFailure(EdgeLprSendResult result)
    {
        if (result.Accepted || !string.Equals(result.Code, "NAK", StringComparison.OrdinalIgnoreCase))
            return false;
        return result.ResultCode is not null && PermanentErrors.Contains(result.ResultCode);
    }

    private static bool TryParseEventId(string value, out Guid eventId)
    {
        eventId = Guid.Empty;
        return value.Length == 32 &&
               value.All(Uri.IsHexDigit) &&
               Guid.TryParseExact(value, "N", out eventId);
    }

    private static bool IsResultCode(string value) =>
        value.Length > 0 && value.All(character =>
            character == '_' ||
            character is >= 'A' and <= 'Z' ||
            character is >= '0' and <= '9');

    public static async Task<string> ReadAsync(Stream stream, CancellationToken token)
    {
        byte[] buffer = new byte[256];
        using MemoryStream data = new();
        while (data.Length < 1024)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(), token);
            if (read == 0) break;
            data.Write(buffer, 0, read);
            string value = Encoding.ASCII.GetString(data.GetBuffer(), 0, (int)data.Length);
            if (value.Contains((char)0x03) || value.Contains('\n')) return value;
        }
        return Encoding.ASCII.GetString(data.ToArray());
    }
}
