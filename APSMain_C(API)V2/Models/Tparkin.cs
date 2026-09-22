using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tparkin
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Ticketdata { get; set; }

    public int? Ticketnum { get; set; }

    public short? Ticketcartype { get; set; }

    public short? Parkcartype { get; set; }

    public string? Carnum { get; set; }

    public short? Indevicenum { get; set; }

    public DateTime Indate { get; set; }

    public short Inhour { get; set; }

    public short Inmin { get; set; }

    public string? Inimage { get; set; }

    public string? Parkonplace { get; set; }

    public sbyte? Outflag { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }
}
