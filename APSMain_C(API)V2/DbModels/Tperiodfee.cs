using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tperiodfee
{
    public int Xindex { get; set; }

    public string Carnum { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Telnum { get; set; } = null!;

    public DateTime Indate { get; set; }

    public DateTime Outdate { get; set; }

    public short Parktype { get; set; }

    public int Parktime { get; set; }

    public int Parkmoney { get; set; }

    public short Collection { get; set; }
}
