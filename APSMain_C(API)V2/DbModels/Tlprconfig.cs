using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tlprconfig
{
    public string Lprip { get; set; } = null!;

    public short? Relayport { get; set; }

    public short? Ldmport { get; set; }

    public short? Tdport { get; set; }

    public string? Name { get; set; }

    public short? Iotype { get; set; }

    public short? Relaynum { get; set; }

    public short? Comuse { get; set; }

    public short? Connection { get; set; }

    public string? Parkarea { get; set; }

    public int? Address { get; set; }

    public int? Aptcall { get; set; }

    public int? Savedata { get; set; }
}
