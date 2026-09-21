using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparkinfo
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public string? Ticketdata { get; set; }

    public int? Ticketnum { get; set; }

    public short? Ticketcartype { get; set; }

    public short? Parkcartype { get; set; }

    public string Carnum { get; set; } = null!;

    public short? Indevicenum { get; set; }

    public DateTime Indate { get; set; }

    public short Inhour { get; set; }

    public short Inmin { get; set; }

    public short? Outdevicenum { get; set; }

    public DateTime Outdate { get; set; }

    public short? Outhour { get; set; }

    public short? Outmin { get; set; }

    public string? Inimage { get; set; }

    public string? Outimage { get; set; }

    public int Parktime { get; set; }

    public sbyte Parkcaltype { get; set; }

    public int Parkmoney { get; set; }

    public int Salemoney { get; set; }

    public int Saletime { get; set; }

    public int? Salepercent { get; set; }

    public int Credittype { get; set; }

    public string? Creditcardno { get; set; }

    public string? Acceptno { get; set; }

    public DateTime? Accepttime { get; set; }

    public int Creditmoney { get; set; }

    public int Receiptnum { get; set; }

    public string? Ocssalecode { get; set; }

    public int Ocssaletype { get; set; }

    public int Ocssaleval { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public string? Saleimage { get; set; }

    public int Intick { get; set; }

    public int Outtick { get; set; }

    public short Manflag { get; set; }

    public short Dendflag { get; set; }

    public short Tendflag { get; set; }

    public DateTime? Denddate { get; set; } = DateTime.Parse("2025-01-01");

    public DateTime? Tenddate { get; set; } = DateTime.Parse("2025-01-01");

    public sbyte Outflag { get; set; }

    public string? Backimage { get; set; }

    public void CopyFrom(Tparkinfo? src)
    {
        if (src == null) { return; }
        Xindex = src.Xindex;
        Sitenum = src.Sitenum;
        Groupnum = src.Groupnum;
        Ticketdata = src.Ticketdata;
        Ticketnum = src.Ticketnum;
        Ticketcartype = src.Ticketcartype;
        Parkcartype = src.Parkcartype;
        Carnum = src.Carnum;
        Indevicenum = src.Indevicenum;
        Indate = src.Indate;
        Inhour = src.Inhour;
        Inmin = src.Inmin;
        Outdevicenum = src.Outdevicenum;
        Outdate = src.Outdate;
        Outhour = src.Outhour;
        Outmin = src.Outmin;
        Inimage = src.Inimage;
        Outimage = src.Outimage;
        Parktime = src.Parktime;
        Parkcaltype = src.Parkcaltype;
        Parkmoney = src.Parkmoney;
        Salemoney = src.Salemoney;
        Saletime = src.Saletime;
        Salepercent = src.Salepercent;
        Credittype = src.Credittype;
        Creditcardno = src.Creditcardno;
        Acceptno = src.Acceptno;
        Accepttime = src.Accepttime;
        Creditmoney = src.Creditmoney;
        Receiptnum = src.Receiptnum;
        Ocssalecode = src.Ocssalecode;
        Ocssaletype = src.Ocssaletype;
        Ocssaleval = src.Ocssaleval;
        Managercode = src.Managercode;
        Managername = src.Managername;
        Saleimage = src.Saleimage;
        Intick = src.Intick;
        Outtick = src.Outtick;
        Manflag = src.Manflag;
        Dendflag = src.Dendflag;
        Tendflag = src.Tendflag;
        Denddate = src.Denddate;
        Tenddate = src.Tenddate;
        Outflag = src.Outflag;
        Backimage = src.Backimage;
    }

    public void Clear()
    {
        Xindex = 0;
        Sitenum = null;
        Groupnum = null;
        Ticketdata = null;
        Ticketnum = null;
        Ticketcartype = null;
        Parkcartype = null;
        Carnum = "";
        Indevicenum = null;
        Indate = default;
        Inhour = 0;
        Inmin = 0;
        Outdevicenum = null;
        Outdate = default;
        Outhour = null;
        Outmin = null;
        Inimage = null;
        Outimage = null;
        Parktime = 0;
        Parkcaltype = 0;
        Parkmoney = 0;
        Salemoney = 0;
        Saletime = 0;
        Salepercent = 0;
        Credittype = 0;
        Creditcardno = null;
        Acceptno = null;
        Accepttime = null;
        Creditmoney = 0;
        Receiptnum = 0;
        Ocssalecode = null;
        Ocssaletype = 0;
        Ocssaleval = 0;
        Managercode = null;
        Managername = null;
        Saleimage = null;
        Intick = 0;
        Outtick = 0;
        Manflag = 0;
        Dendflag = 0;
        Tendflag = 0;
        Denddate = DateTime.Parse("2025-01-01");
        Tenddate = DateTime.Parse("2025-01-01");
        Outflag = 0;
        Backimage = null;
    }
}
