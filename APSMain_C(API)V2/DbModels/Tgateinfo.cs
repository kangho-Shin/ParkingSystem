using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tgateinfo
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public string? Gatename { get; set; }

    public string? Ip { get; set; }

    public short? Inouttype { get; set; }

    public short? Openclose { get; set; }

    public DateTime Opdate { get; set; }

    public short? Ophour { get; set; }

    public short? Opmin { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public short? Manflag { get; set; }

    public short? Tendflag { get; set; }

    public short? Dendflag { get; set; }
}
