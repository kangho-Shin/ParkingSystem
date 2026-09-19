namespace Parking.Contracts;

public sealed class PeriodMemberDetail
{
    public long MemberId { get; set; }
    public long SiteId { get; set; }
    public int Groupnum { get; set; }
    public int DeviceId { get; set; }
    public long CardId { get; set; }
    public string? SerialNo { get; set; }
    public short PeriodType { get; set; }
    public string? Name { get; set; }
    public string? TelNumber { get; set; }
    public string? GroupCode { get; set; }
    public string Company1 { get; set; } = "";
    public string Company2 { get; set; } = "";
    public string CarNumber1 { get; set; } = "";
    public string CarType1 { get; set; } = "";
    public string CarNumber2 { get; set; } = "";
    public string CarType2 { get; set; } = "";
    public string Address { get; set; } = "";
    public short ParkType { get; set; }
    public DateTime? RecordDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public short ParkTimeCode { get; set; }
    public string? ParkTimeTime { get; set; }
    public int ParkPrice { get; set; }
    public string? ParkArea { get; set; }
    public int ParkLevel { get; set; }
    public string? ParkValidDay { get; set; }
    public short AntiFlag { get; set; }
    public short UseFlag { get; set; }
    public short ServiceDay { get; set; }
    public short ManagerCode { get; set; }
    public string? ManagerName { get; set; }
    public string? PayType { get; set; }
    public string OutFlag { get; set; } = "I";
    public int? InTimeTick { get; set; }
    public short Reserved1 { get; set; }
    public short Reserved2 { get; set; }
    public short Reserved3 { get; set; }
    public short Reserved4 { get; set; }
    public string? Note { get; set; }
}

public sealed class PeriodMemberSaveRequest
{
    public long SiteId { get; set; }
    public int Groupnum { get; set; }
    public int DeviceId { get; set; }
    public long CardId { get; set; }
    public string? SerialNo { get; set; }
    public short PeriodType { get; set; }
    public string? Name { get; set; }
    public string? TelNumber { get; set; }
    public string? GroupCode { get; set; }
    public string Company1 { get; set; } = "";
    public string Company2 { get; set; } = "";
    public string CarNumber1 { get; set; } = "";
    public string CarType1 { get; set; } = "";
    public string CarNumber2 { get; set; } = "";
    public string CarType2 { get; set; } = "";
    public string Address { get; set; } = "";
    public short ParkType { get; set; }
    public DateTime? RecordDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public short ParkTimeCode { get; set; }
    public string? ParkTimeTime { get; set; }
    public int ParkPrice { get; set; }
    public string? ParkArea { get; set; }
    public int ParkLevel { get; set; }
    public string? ParkValidDay { get; set; }
    public short AntiFlag { get; set; }
    public short UseFlag { get; set; }
    public short ServiceDay { get; set; }
    public short ManagerCode { get; set; }
    public string? ManagerName { get; set; }
    public string? PayType { get; set; }
    public string OutFlag { get; set; } = "I";
    public int? InTimeTick { get; set; }
    public short Reserved1 { get; set; }
    public short Reserved2 { get; set; }
    public short Reserved3 { get; set; }
    public short Reserved4 { get; set; }
    public string? Note { get; set; }
}
