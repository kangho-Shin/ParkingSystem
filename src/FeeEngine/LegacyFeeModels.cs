using Newtonsoft.Json;

namespace Parking.FeeEngine;

public sealed class Tparkfee
{
    [JsonProperty("sitenum")] public int Sitenum { get; set; }
    [JsonProperty("groupnum")] public int Groupnum { get; set; }
    [JsonProperty("weektype")] public int Weektype { get; set; }
    [JsonProperty("dayshift")] public int Dayshift { get; set; }
    [JsonProperty("cartype")] public int Cartype { get; set; }
    [JsonProperty("feestep")] public int Feestep { get; set; }
    [JsonProperty("parktime")] public int? Parktime { get; set; }
    [JsonProperty("parkfee")] public int? Parkfee { get; set; }
    [JsonProperty("maxcount")] public int? Maxcount { get; set; }
}

public sealed class Tdiscount
{
    [JsonProperty("sitenum")] public int Sitenum { get; set; }
    [JsonProperty("groupnum")] public int Groupnum { get; set; }
    [JsonProperty("key")] public int Key { get; set; }
    [JsonProperty("type")] public int Type { get; set; }
    [JsonProperty("value")] public int? Value { get; set; }
}

public sealed class Tholiday
{
    [JsonProperty("sitenum")] public int Sitenum { get; set; }
    [JsonProperty("groupnum")] public int Groupnum { get; set; }
    [JsonProperty("hdate")] public DateTime Hdate { get; set; }
}

public sealed class Tparkvariable
{
    [JsonProperty("sitenum")] public int Sitenum { get; set; }
    [JsonProperty("groupnum")] public int Groupnum { get; set; }
    [JsonProperty("cmdtype")] public string Cmdtype { get; set; } = "";
    [JsonProperty("val")] public string? Val { get; set; }
    [JsonProperty("opt")] public string? Opt { get; set; }
    [JsonProperty("msg")] public string? Msg { get; set; }
}
