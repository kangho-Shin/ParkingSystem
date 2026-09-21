using System.Collections.Specialized;
using System.Configuration;

namespace APSMain.Integration.EdgeService;

public sealed record EdgeServiceOptions(Uri BaseAddress, long Sitenum, int Groupnum, int Devicenum)
{
    public static EdgeServiceOptions Load(NameValueCollection values)
    {
        string rawUrl = values["EDGESERVICEURL"] ?? "http://localhost:5200";
        if (!Uri.TryCreate(rawUrl.TrimEnd('/') + "/", UriKind.Absolute, out Uri? baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            throw new ConfigurationErrorsException("EDGESERVICEURL 설정이 올바르지 않습니다.");

        return new EdgeServiceOptions(
            baseAddress,
            ReadPositiveLong(values, "SITENUM"),
            ReadPositiveInt(values, "GROUPNUM"),
            ReadPositiveInt(values, "APSNUM"));
    }

    private static long ReadPositiveLong(NameValueCollection values, string key) =>
        long.TryParse(values[key], out long value) && value > 0
            ? value
            : throw new ConfigurationErrorsException($"{key} 설정은 0보다 큰 정수여야 합니다.");

    private static int ReadPositiveInt(NameValueCollection values, string key) =>
        int.TryParse(values[key], out int value) && value > 0
            ? value
            : throw new ConfigurationErrorsException($"{key} 설정은 1부터 {int.MaxValue} 사이의 정수여야 합니다.");
}
