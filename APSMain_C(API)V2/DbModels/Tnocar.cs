using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tnocar
{
    public int Xindex { get; set; }

    public int? Ionum { get; set; }

    public string? Ioname { get; set; }

    public string Iodate { get; set; } = null!;

    public string? Image { get; set; }

    public int? Outflag { get; set; }
}
