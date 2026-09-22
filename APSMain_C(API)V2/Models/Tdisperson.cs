using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tdisperson
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Disid { get; set; }

    public string? Name { get; set; }

    public string Carnum { get; set; } = null!;

    public DateTime? Recorddate { get; set; }

    public DateTime? Startdate { get; set; }

    public DateTime? Enddate { get; set; }

    public int? Salekey { get; set; }

    public short? Saletype { get; set; }

    public int? Saleval { get; set; }

    public string? Telnum { get; set; }

    public string? Salemsg { get; set; }
}
