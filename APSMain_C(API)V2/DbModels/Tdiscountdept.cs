using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscountdept
{
    public int Deptcode { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Deptname { get; set; }

    public int? Salecount { get; set; }

    public int? Carcount { get; set; }

    public int? Salemaxnum { get; set; }

    public int? Carmaxnum { get; set; }

    public DateTime? Regdate { get; set; }
}
