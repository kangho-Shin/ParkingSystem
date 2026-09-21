using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tperiodtmember
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public int? Cardid { get; set; }

    public string? Name { get; set; }

    public string? Telnum { get; set; }

    public string Carnum { get; set; } = null!;

    public string? Visitobject { get; set; }

    public string? Visitplace { get; set; }

    public int? Ticketnum { get; set; }

    public DateTime? Regdate { get; set; }

    public DateTime? Startdate { get; set; }

    public DateTime? Enddate { get; set; }

    public short? Saletype { get; set; }

    public int? Saleval { get; set; }

    public string? Ticketdata { get; set; }

    public int? Pindex { get; set; }

    public string? Id { get; set; }
}
