using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

public partial class Tperiodparktime
{
    public short Sitenum { get; set; }

    public short Groupnum { get; set; }

    public short Timecode { get; set; }

    public string? Description { get; set; }

    public short? Starthour { get; set; }

    public short? Startmin { get; set; }

    public short? Endhour { get; set; }

    public short? Endmin { get; set; }
}
