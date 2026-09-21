using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdepartment
{
    public short Sitenum { get; set; }

    public short Groupnum { get; set; }

    public short Companycode { get; set; }

    public string? Companyname { get; set; }

    public int Deptcode { get; set; }

    public string? Deptname { get; set; }

    public string? Depttel { get; set; }

    public string? Deptfax { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }
}
