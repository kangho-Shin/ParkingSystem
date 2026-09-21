using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tperiodaccount
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public short? Devicetype { get; set; }

    public int? Cardid { get; set; }

    public string? Name { get; set; }

    public string? Carnum { get; set; }

    public string? Company1 { get; set; }

    public string? Company2 { get; set; }

    public DateTime? Recorddate { get; set; }

    public DateTime? Startdate { get; set; }

    public short? Starthour { get; set; }

    public short? Startmin { get; set; }

    public DateTime? Enddate { get; set; }

    public short? Endhour { get; set; }

    public short? Endmin { get; set; }

    public int? Price { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public short? Printflag { get; set; }

    public short? Manflag { get; set; }

    public short? Dendflag { get; set; }

    public int? Paytype { get; set; }

    public string? Memo { get; set; }

    public string? Salemsg { get; set; }
}
