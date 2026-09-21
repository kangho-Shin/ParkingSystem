using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscountinfo
{
    public int Xindex { get; set; }

    public string? Logid { get; set; }

    public int? Devicenum { get; set; }

    public string? Carnum { get; set; }

    public int? Deptcode { get; set; }

    public int? Salecode { get; set; }

    public int? Saletype { get; set; }

    public int? Salevalue { get; set; }

    public DateTime? Indate { get; set; }

    public DateTime Sdate { get; set; }

    public string? Remoteip { get; set; }

    public int? Tparkindex { get; set; }

    public int? Salemoney { get; set; }
}
