using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscountissue
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Issuedept { get; set; }

    public string? Issuenote { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public DateTime Issuedate { get; set; }

    public short? Saletype { get; set; }

    public int? Saleval { get; set; }

    public int? Startnum { get; set; }

    public int? Endnum { get; set; }

    public string? Paymenttype { get; set; }

    public int? Price { get; set; }
}
