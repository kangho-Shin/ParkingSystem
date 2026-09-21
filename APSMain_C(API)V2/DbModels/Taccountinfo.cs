using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Taccountinfo
{
    public int Xindex { get; set; }

    public string Id { get; set; } = null!;

    public int Salecode { get; set; }

    public int? Salecount { get; set; }

    public int? Salemaxnum { get; set; }

    public DateTime? Regdate { get; set; }
}
