using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Txblacklist
{
    public int Xindex { get; set; }

    public string Carnum { get; set; } = null!;

    public string? Name { get; set; }

    public string? Telnum { get; set; }

    public DateTime? Regdate { get; set; }

    public DateTime? Startdate { get; set; }

    public DateTime? Enddate { get; set; }

    public string? Msg { get; set; }
}
