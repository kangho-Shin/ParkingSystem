using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdevicelist
{
    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public string? Devicename { get; set; }

    public short? Devicetype { get; set; }

    public string Ip { get; set; } = null!;

    public string? Linkip { get; set; }

    public short? Iotype { get; set; }

    public int? Income { get; set; }

    public int? Restmoney0 { get; set; }

    public int? Restmoney1 { get; set; }

    public int? Restmoney2 { get; set; }

    public int? Restmoney3 { get; set; }

    public int? Restmoney4 { get; set; }

    public int? Restmoney5 { get; set; }

    public DateTime? Connecttime { get; set; }

    public int? Connect { get; set; }

    public string? Note { get; set; }
}
