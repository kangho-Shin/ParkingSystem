using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdevicealarm
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public string? Alarmkind { get; set; }

    public DateTime Alarmtime { get; set; }

    public DateTime? Repairtime { get; set; }

    public string? Alarmmessage { get; set; }

    public string? Repairmessage { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }
}
