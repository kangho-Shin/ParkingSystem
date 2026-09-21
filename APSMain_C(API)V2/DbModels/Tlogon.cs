using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tlogon
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public short? Devicetype { get; set; }

    public short? Managercode { get; set; }

    public string? Managerid { get; set; }

    public string? Managerpass { get; set; }

    public string? Managername { get; set; }

    public DateTime? Logdatetime { get; set; }

    public short? Logtype { get; set; }
}
