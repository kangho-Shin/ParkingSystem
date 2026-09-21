using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tapswelfare
{
    public int Xindex { get; set; }

    public int? Sitenum { get; set; }

    public int? Groupnum { get; set; }

    public int Apsnum { get; set; }

    public string Apsip { get; set; } = null!;

    public string? Apsname { get; set; }

    public string? Status { get; set; }

    public byte[]? Parkinfo { get; set; }
}
