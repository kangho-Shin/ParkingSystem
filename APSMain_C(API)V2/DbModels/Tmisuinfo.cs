using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tmisuinfo
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Carnum { get; set; }

    public DateTime? Indate { get; set; }

    public DateTime? Outdate { get; set; }

    public short? Misu { get; set; }

    public int? Parkmoney { get; set; }

    public int? Salemoney { get; set; }

    public int? Pindex { get; set; }
}
