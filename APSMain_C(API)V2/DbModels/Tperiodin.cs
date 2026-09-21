using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tperiodin
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

    public DateTime? Enddate { get; set; }

    public int? Intimetick { get; set; }

    public sbyte? Outflag { get; set; }

    public string? Parkonplace { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }
}
