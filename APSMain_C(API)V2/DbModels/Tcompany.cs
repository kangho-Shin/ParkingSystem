using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tcompany
{
    public short Sitenum { get; set; }

    public short Groupnum { get; set; }

    public short Companycode { get; set; }

    public string? Companyname { get; set; }

    public string? Pregidentname { get; set; }

    public string? Companyaddress { get; set; }

    public string? Companytel { get; set; }

    public string? Companyfax { get; set; }

    public short? Managercode { get; set; }

    public string? Managername { get; set; }

    public string? Note { get; set; }
}
