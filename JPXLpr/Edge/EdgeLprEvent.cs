namespace JPXLpr.Edge;

public sealed record EdgeLprEvent(
    Guid EventId, int Sitenum, int Groupnum, int Devicenum, int Laneid,
    string Direction, DateTime EventTime, string CarNumber, string FileName)
{
    public static EdgeLprEvent Create(
        int sitenum, int groupnum, int devicenum, int laneid, string direction,
        DateTime eventTime, string carNumber, Guid? eventId = null)
    {
        EdgeLprOptions identity = new(sitenum, groupnum, laneid, devicenum, direction, "localhost", 29200);
        identity.Validate();
        Guid id = eventId ?? Guid.NewGuid();
        string safeCarNumber = Sanitize(carNumber);
        string fileName = $"{sitenum:D3}_{groupnum:D3}_{devicenum:D3}_{laneid:D4}_{direction}_{eventTime:yyyyMMddHHmmssfff}_{safeCarNumber}_{id:N}.jpg";
        return new(id, sitenum, groupnum, devicenum, laneid, direction, eventTime, safeCarNumber, fileName);
    }

    private static string Sanitize(string value)
    {
        string result = string.Concat((value ?? string.Empty).Trim()
            .Where(c => !char.IsWhiteSpace(c) && !Path.GetInvalidFileNameChars().Contains(c) && c != '_'));
        return string.IsNullOrWhiteSpace(result) ? "UNKNOWN" : result;
    }
}
