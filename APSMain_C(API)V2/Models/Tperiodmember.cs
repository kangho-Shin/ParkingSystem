using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tperiodmember
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public int? Cardid { get; set; }

    public string? Serialno { get; set; }

    public short? Periodtype { get; set; }

    public string? Name { get; set; }

    public string? Telnum { get; set; }

    public string? Groupcode { get; set; }

    public string? Company1 { get; set; }

    public string? Company2 { get; set; }

    public string Carnum1 { get; set; } = null!;

    public string? Cartype1 { get; set; }

    public string? Carnum2 { get; set; }

    public string? Cartype2 { get; set; }

    public string? Address { get; set; }

    public short? Parktype { get; set; }

    public DateTime Recorddate { get; set; }

    public DateTime Startdate { get; set; }

    public DateTime Enddate { get; set; }

    public short? Parktimecode { get; set; }

    public string? Parktimetime { get; set; }

    public int? Parkprice { get; set; }

    public string? Parkarea { get; set; }

    public int? Parklevel { get; set; }

    public string? Parkvalidday { get; set; }

    public short? Antiflag { get; set; }

    public short? Useflag { get; set; }

    public short? Serviceday { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public string? Paytype { get; set; }

    public sbyte? Outflag { get; set; }

    public int? Intimetick { get; set; }

    public short? Reserved1 { get; set; }

    public short? Reserved2 { get; set; }

    public short? Reserved3 { get; set; }

    public short? Reserved4 { get; set; }

    public string? Note { get; set; }

    public void CopyFrom(Tperiodmember? src)
    {
        if (src == null) { return; }
        Xindex = src.Xindex;
        Sitenum = src.Sitenum;
        Groupnum = src.Groupnum;
        Devicenum = src.Devicenum;
        Cardid = src.Cardid;
        Serialno = src.Serialno;
        Periodtype = src.Periodtype;
        Name = src.Name;
        Telnum = src.Telnum;
        Groupcode = src.Groupcode;
        Company1 = src.Company1;
        Company2 = src.Company2;
        Carnum1 = src.Carnum1;
        Cartype1 = src.Cartype1;
        Carnum2 = src.Carnum2;
        Cartype2 = src.Cartype2;
        Address = src.Address;
        Parktype = src.Parktype;
        Recorddate = src.Recorddate;
        Startdate = src.Startdate;
        Enddate = src.Enddate;
        Parktimecode = src.Parktimecode;
        Parktimetime = src.Parktimetime;
        Parkprice = src.Parkprice;
        Parkarea = src.Parkarea;
        Parklevel = src.Parklevel;
        Parkvalidday = src.Parkvalidday;
        Antiflag = src.Antiflag;
        Useflag = src.Useflag;
        Serviceday = src.Serviceday;
        Managercode = src.Managercode;
        Managername = src.Managername;
        Paytype = src.Paytype;
        Outflag = src.Outflag;
        Intimetick = src.Intimetick;
        Reserved1 = src.Reserved1;
        Reserved2 = src.Reserved2;
        Reserved3 = src.Reserved3;
        Reserved4 = src.Reserved4;
        Note = src.Note;
    }

    public void Clear()
    {
        Xindex = 0;
        Sitenum = null;
        Groupnum = null;
        Devicenum = null;
        Cardid = null;
        Serialno = null;
        Periodtype = null;
        Name = null;
        Telnum = null;
        Groupcode = null;
        Company1 = null;
        Company2 = null;
        Carnum1 = "";
        Cartype1 = null;
        Carnum2 = null;
        Cartype2 = null;
        Address = null;
        Parktype = null;
        Recorddate = default;
        Startdate = default;
        Enddate = default;
        Parktimecode = null;
        Parktimetime = null;
        Parkprice = null;
        Parkarea = null;
        Parklevel = null;
        Parkvalidday = null;
        Antiflag = null;
        Useflag = null;
        Serviceday = null;
        Managercode = null;
        Managername = null;
        Paytype = null;
        Outflag = null;
        Intimetick = null;
        Reserved1 = null;
        Reserved2 = null;
        Reserved3 = null;
        Reserved4 = null;
        Note = null;
    }
}
