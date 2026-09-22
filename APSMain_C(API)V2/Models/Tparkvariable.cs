using System;
using System.Collections.Generic;

namespace APSMain.Models;

public partial class Tparkvariable
{
    public int Xindex { get; set; }

    public int? Sitenum { get; set; }

    public int? Groupnum { get; set; }

    public string? Cmd_Type { get; set; }

    public string? Val { get; set; }

    public string? Opt { get; set; }

    public DateTime? Regdate { get; set; }

    public string? Msg { get; set; }
}
