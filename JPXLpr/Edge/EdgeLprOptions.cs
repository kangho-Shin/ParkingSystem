namespace JPXLpr.Edge;

public sealed record EdgeLprOptions(
    int Sitenum, int Groupnum, int Laneid, int Devicenum,
    string Direction, string Host, int Port)
{
    public void Validate()
    {
        if (Sitenum <= 0 || Groupnum is <= 0 or > 999 || Laneid <= 0 || Devicenum is <= 0 or > 999)
            throw new InvalidOperationException("LPR 식별정보가 올바르지 않습니다.");
        if (Direction is not ("Entry" or "Exit"))
            throw new InvalidOperationException("DIRECTION은 Entry 또는 Exit여야 합니다.");
        if (string.IsNullOrWhiteSpace(Host) || Port is < 1 or > 65535)
            throw new InvalidOperationException("EdgeService 주소가 올바르지 않습니다.");
    }
}
