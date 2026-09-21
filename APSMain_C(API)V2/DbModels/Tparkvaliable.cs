using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tparkvaliable
{
    public int Xindex { get; set; }

    public string Cmd_Type { get; set; } = null!;

    public string Val { get; set; } = null!;

    public string Opt { get; set; } = null!;

    public DateTime? Regdate { get; set; }

    public string Msg { get; set; } = null!;
}
