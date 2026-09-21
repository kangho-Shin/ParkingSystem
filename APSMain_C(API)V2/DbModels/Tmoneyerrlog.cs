using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tmoneyerrlog
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public DateTime? Errdate { get; set; }

    public string? Filename { get; set; }

    public string? Errlog { get; set; }
}
