using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tdiscounttable
{
    public int Salecode { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public int? Saletype { get; set; }

    public int? Salevalue { get; set; }

    public string? Saletitle { get; set; }

    public int? Ticketon { get; set; }

    public int? Limitval { get; set; }

    public DateTime? Regdate { get; set; }
}
