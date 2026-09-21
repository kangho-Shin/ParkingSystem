using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tmanager
{
    public int Xindex { get; set; }

    public short? Sitenum { get; set; }

    public short? Groupnum { get; set; }

    public short? Devicenum { get; set; }

    public short? Managercode { get; set; }

    public string? Managerid { get; set; }

    public string? Managerpw { get; set; }

    public string? Managername { get; set; }

    public string? Managertel { get; set; }

    public string? Managerplace { get; set; }

    public short? Managerpower { get; set; }

    public TimeOnly? Workingend { get; set; }

    public TimeOnly? Workingstart { get; set; }
}
