using System.Collections.Specialized;
using System.Configuration;
using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class EdgeServiceOptionsTests
{
    [Fact]
    public void Load_uses_local_default_and_reads_identity()
    {
        NameValueCollection values = new() { ["SITENUM"] = "9001", ["GROUPNUM"] = "2", ["APSNUM"] = "201" };

        EdgeServiceOptions options = EdgeServiceOptions.Load(values);

        Assert.Equal(new Uri("http://localhost:5200/"), options.BaseAddress);
        Assert.Equal(9001, options.Sitenum);
        Assert.Equal(2, options.Groupnum);
        Assert.Equal(201, options.Devicenum);
    }

    [Theory]
    [InlineData("SITENUM", "")]
    [InlineData("GROUPNUM", "0")]
    [InlineData("APSNUM", "0")]
    public void Load_rejects_missing_or_non_positive_identity(string key, string value)
    {
        NameValueCollection values = new() { ["SITENUM"] = "9001", ["GROUPNUM"] = "2", ["APSNUM"] = "201" };
        values[key] = value;

        Assert.Throws<ConfigurationErrorsException>(() => EdgeServiceOptions.Load(values));
    }
}
