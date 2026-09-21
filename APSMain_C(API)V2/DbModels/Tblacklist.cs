using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tblacklist
{
    public int Xindex { get; set; }

    public string Carnum { get; set; } = null!;

    public string? Indatetime { get; set; }

    public string? Outdatetime { get; set; }

    public int? Indevicenum { get; set; }

    public int? Outdevicenum { get; set; }

    public int? Parkmoney { get; set; }

    public string? Inimage { get; set; }

    public string? Outimage { get; set; }
}
