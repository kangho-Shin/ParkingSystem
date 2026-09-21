using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tnocarnum
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public int? Ticketnum { get; set; }

    public string? Imgname { get; set; }

    public DateTime? Indate { get; set; }

    public int? Outflag { get; set; }
}
