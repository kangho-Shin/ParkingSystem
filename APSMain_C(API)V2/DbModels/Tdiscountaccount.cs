using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tdiscountaccount
{
    public int Xindex { get; set; }

    public string Id { get; set; } = null!;

    public string? Telnum { get; set; }

    public string? Name { get; set; }

    public string Password { get; set; } = null!;

    public int? Grade { get; set; }

    public DateTime? Regdate { get; set; }

    public int Deptcode { get; set; }
}
