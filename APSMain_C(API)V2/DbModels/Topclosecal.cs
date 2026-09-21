using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Topclosecal
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public DateTime? Edate { get; set; }

    public TimeOnly? Etime { get; set; }

    public short? Etype { get; set; }

    public string? Manid { get; set; }

    public short? Device { get; set; }

    public short? Mainclass { get; set; }

    public short? Midclass { get; set; }

    public short? Subclass { get; set; }

    public short? Devicenum { get; set; }

    public int? Totalnum { get; set; }

    public int? Salemoney { get; set; }

    public int? Parkmoney { get; set; }

    public int? Parkprice { get; set; }

    public int? Creditnum { get; set; }

    public int? Creditprice { get; set; }

    public int? Tmoneynum { get; set; }

    public int? Tmoneyprice { get; set; }
}
