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
        "DIRECTION_MISMATCH"
    };

    public static EdgeLprSendResult Match(string response, Guid eventId)
    {
        string value = response.Trim().Trim('\0', '\r', '\n', (char)0x02, (char)0x03);
        if (value.StartsWith("NAK", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("NACK", StringComparison.OrdinalIgnoreCase))
            return new(false, "NAK", value);
        if (!value.StartsWith("ACK|", StringComparison.OrdinalIgnoreCase))
            return new(false, "INVALID_RESPONSE", value);
        string idText = value[4..].Trim();
        if (!Guid.TryParse(idText, out Guid actual) || actual != eventId)
            return new(false, "ACK_EVENT_MISMATCH", value);
        return new(true, "ACK", value);
    }

    public static bool IsPermanentFailure(EdgeLprSendResult result)
    {
        if (result.Accepted || !string.Equals(result.Code, "NAK", StringComparison.OrdinalIgnoreCase))
            return false;
        string errorCode = result.Message.Split('|').LastOrDefault()?.Trim() ?? string.Empty;
        return PermanentErrors.Contains(errorCode);
    }

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
