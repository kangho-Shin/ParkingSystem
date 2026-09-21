using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Ttcardinfo
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Ticketdata { get; set; }

    public short? Outdevicenum { get; set; }

    public string? Termid { get; set; }

    public string? Posid { get; set; }

    public string? Rescode { get; set; }

    public string? Dealnum { get; set; }

    public DateTime Dealdate { get; set; }

    public string? Dealtime { get; set; }

    public string? Cardname { get; set; }

    public string? Cardid { get; set; }

    public string? Branchnum { get; set; }

    public string? Acceptnum { get; set; }

    public string? Samid { get; set; }

    public string? Samdealnum { get; set; }

    public string? Receiptnum { get; set; }

    public short? Accepttype { get; set; }

    public int? Money { get; set; }

    public int? Parktime { get; set; }

    public short? Dendflag { get; set; }

    public short? Tendflag { get; set; }

    public DateTime? Enddate { get; set; }

    public int? Paytype { get; set; }
}
