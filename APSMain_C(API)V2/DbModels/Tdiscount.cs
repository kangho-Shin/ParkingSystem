using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscount
{
    public int Xindex { get; set; }

    public int? Sitenum { get; set; }

    public int? Groupnum { get; set; }

    public int Key { get; set; }

    public int? Type { get; set; }

    public int? Value { get; set; }

    public string? Title { get; set; }

    public int? Maxcount { get; set; }

    public DateTime? Regdate { get; set; }

    public DateTime? Moddate { get; set; }

    public string? Mid { get; set; }
}
