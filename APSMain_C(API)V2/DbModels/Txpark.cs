using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Txpark
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

    public DateTime? Indate { get; set; }

    public short? Outdevicenum { get; set; }

    public DateTime Outdate { get; set; }

    public string? Inimage { get; set; }

    public string? Outimage { get; set; }

    public int? Parktime { get; set; }

    public sbyte? Parkcaltype { get; set; }

    public int? Parkmoney { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public int? Intick { get; set; }

    public int? Outtick { get; set; }

    public int? Pindex { get; set; }
}
