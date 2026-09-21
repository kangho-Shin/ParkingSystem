using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Taccept
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public short? Devicetype { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public string? Ip { get; set; }

    public DateTime Accepttime { get; set; }

    public short? Accepttype { get; set; }
}
