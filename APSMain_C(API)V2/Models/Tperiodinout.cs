using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tperiodinout
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public int? Cardid { get; set; }

    public string? Name { get; set; }

    public string? Carnum { get; set; }

    public string? Cartype { get; set; }

    public short? Indevicenum { get; set; }

    public DateTime Indate { get; set; }

    public short? Inhour { get; set; }

    public short? Inmin { get; set; }

    public string? Inimage { get; set; }

    public short? Outdevicenum { get; set; }

    public DateTime Outdate { get; set; }

    public short? Outhour { get; set; }

    public short? Outmin { get; set; }

    public string? Outimage { get; set; }

    public int? Parktime { get; set; }

    public DateTime? Enddate { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public sbyte? Outflag { get; set; }

    public string? Note { get; set; }

    public short? Parktimecode { get; set; }

    public string? Parktimetime { get; set; }

    public string? Backimage { get; set; }
}
