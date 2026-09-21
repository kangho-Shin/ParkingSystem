using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparkemergency
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Indevicenum { get; set; }

    public short? Outdevicenum { get; set; }

    public string? Carnum { get; set; }

    public DateTime Indate { get; set; }

    public DateTime? Outdate { get; set; }

    public string? Inimage { get; set; }

    public string? Outimage { get; set; }

    public short? Outflag { get; set; }

    public string? Msg { get; set; }
}
