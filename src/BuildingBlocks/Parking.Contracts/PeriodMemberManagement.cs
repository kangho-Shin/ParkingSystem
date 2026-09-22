namespace Parking.Contracts;

public sealed class PeriodMemberDetail
{
    public long MemberId { get; set; }
    public long SiteId { get; set; }
    public int Groupnum { get; set; }
    public int DeviceNumber { get; set; }
    public long CardNumber { get; set; }
    public string? SerialNo { get; set; }
    public int PeriodType { get; set; }
    public int GroupCode { get; set; }
    public string CarNumber1 { get; set; } = "";
    public int CarType1 { get; set; } = 1;
    public string? CarNumber2 { get; set; }
    public int? CarType2 { get; set; }
    public string Name { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Address { get; set; }
    public int? CompanyCode { get; set; }
    public int? DepartmentCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ServiceDay { get; set; }
    public int ParkType { get; set; }
    public int ParkTimeCode { get; set; }
    public string ParkArea { get; set; } = "0000000";
    public string ParkValidDay { get; set; } = "1111111";
    public int ParkLevel { get; set; }
    public int ParkPrice { get; set; }
    public int? DiscountKey { get; set; }
    public int PayType { get; set; }
    public int UseFlag { get; set; } = 1;
    public string OutFlag { get; set; } = "O";
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
}

public sealed class PeriodMemberSaveRequest
{
    public long SiteId { get; set; }
    public int Groupnum { get; set; }
    public int DeviceNumber { get; set; }
    public long CardNumber { get; set; }
    public string? SerialNo { get; set; }
    public int PeriodType { get; set; }
    public int GroupCode { get; set; }
    public string CarNumber1 { get; set; } = "";
    public int CarType1 { get; set; } = 1;
    public string? CarNumber2 { get; set; }
    public int? CarType2 { get; set; }
    public string Name { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Address { get; set; }
    public int? CompanyCode { get; set; }
    public int? DepartmentCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ServiceDay { get; set; }
    public int ParkType { get; set; }
    public int ParkTimeCode { get; set; }
    public string ParkArea { get; set; } = "0000000";
    public string ParkValidDay { get; set; } = "1111111";
    public int ParkLevel { get; set; }
    public int ParkPrice { get; set; }
    public int? DiscountKey { get; set; }
    public int PayType { get; set; }
    public int UseFlag { get; set; } = 1;
    public string OutFlag { get; set; } = "O";
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
}
