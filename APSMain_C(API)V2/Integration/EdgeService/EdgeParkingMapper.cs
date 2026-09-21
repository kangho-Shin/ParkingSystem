using APSMain.DbModels;

namespace APSMain.Integration.EdgeService;

public static class EdgeParkingMapper
{
    public static Tparkinfo FromCandidate(ParkingSearchCandidate value, long sitenum) =>
        Create(value.ParkingSessionId, value.CarNumber, sitenum, value.Groupnum,
            value.CarType, value.InDateTime, value.InImage);

    public static Tparkinfo FromQuote(
        FeeQuote value, long sitenum, int groupnum, string? inImage = null, int carType = 1) =>
        Create(value.ParkingSessionId, value.CarNumber, sitenum, groupnum,
            carType, value.EntryAt, inImage);

    private static Tparkinfo Create(
        long sessionId, string carNumber, long sitenum, int groupnum,
        int carType, DateTimeOffset entryAt, string? inImage)
    {
        DateTime entry = entryAt.LocalDateTime;
        return new Tparkinfo
        {
            Xindex = checked((int)sessionId),
            Sitenum = checked((short)sitenum),
            Groupnum = checked((short)groupnum),
            Carnum = carNumber,
            Ticketcartype = checked((short)(carType <= 0 ? 1 : carType)),
            Parkcartype = checked((short)(carType <= 0 ? 1 : carType)),
            Indate = entry,
            Inhour = checked((short)entry.Hour),
            Inmin = checked((short)entry.Minute),
            Inimage = inImage,
            Outflag = 73
        };
    }
}
