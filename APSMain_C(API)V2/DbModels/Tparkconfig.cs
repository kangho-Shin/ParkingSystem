using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparkconfig
{
    public int Sitenum { get; set; }

    public byte[]? Xfile { get; set; }
}
