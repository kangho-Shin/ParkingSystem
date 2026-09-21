using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscountsale
{
    public int Xindex { get; set; }

    public DateTime? Saledate { get; set; }

    public string? Saleshop { get; set; }

    public int? Salecode { get; set; }

    public string? Saletitle { get; set; }

    public int? Salemoney { get; set; }

    public int? Saleamount { get; set; }

    public int? Managercode { get; set; }

    public string? Managername { get; set; }

    public int? Manflag { get; set; }

    public int? Dendflag { get; set; }
}
