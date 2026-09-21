using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparkfee
{
    public int Xindex { get; set; }

    public int? Sitenum { get; set; }

    public int? Groupnum { get; set; }

    public int? Weektype { get; set; }

    public int? Cartype { get; set; }

    public int? Dayshift { get; set; }

    public int? Feestep { get; set; }

    public int? Parktime { get; set; }

    public int? Parkfee { get; set; }

    public int? Maxcount { get; set; }

    public DateTime? Regdate { get; set; }

    public DateTime? Moddate { get; set; }

    public string? Mid { get; set; }
}
