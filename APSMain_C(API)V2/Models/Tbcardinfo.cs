using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tbcardinfo
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

    public string? Receiptnum { get; set; }

    public short? Accepttype { get; set; }

    public int? Money { get; set; }

    public int? Parktime { get; set; }

    public short? Dendflag { get; set; }

    public short? Tendflag { get; set; }

    public DateTime? Enddate { get; set; }

    public int? Parktype { get; set; }

    public DateTime? Intime { get; set; }

    public DateTime? Outtime { get; set; }

    public int? Canclemoney { get; set; }

    public DateTime? Cancletime { get; set; }

    public string? Msg { get; set; }

    public int? Pindex { get; set; }
}
