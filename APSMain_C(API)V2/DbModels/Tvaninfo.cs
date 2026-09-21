using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tvaninfo
{
    public int Xindex { get; set; }

    public int Vancode { get; set; }

    public string? Branchcode { get; set; }

    public string? Branchname { get; set; }
}
