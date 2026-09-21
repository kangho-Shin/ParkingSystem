using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tcuponinfo
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Cuponkind { get; set; }

    public int? Cuponnum { get; set; }

    public DateTime Recorddate { get; set; }

    public DateTime? Startdate { get; set; }

    public DateTime? Enddate { get; set; }

    public int? Applyvalue { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public int? Unitprice { get; set; }
}
