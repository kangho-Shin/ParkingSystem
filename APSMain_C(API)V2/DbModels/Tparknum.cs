using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparknum
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public short? Devicetype { get; set; }

    public uint? Parkinnum { get; set; }

    public uint? Parkoutnum { get; set; }

    public uint? Terminnum { get; set; }

    public uint? Termoutnum { get; set; }

    public uint? Parkfullnum { get; set; }
}
