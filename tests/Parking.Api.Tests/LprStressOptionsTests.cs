using Parking.LprStressTester;

namespace Parking.Api.Tests;

public sealed class LprStressOptionsTests
{
    [Fact]
    public void 인수가_없으면_현장시험_기본값을_사용한다()
    {
        LprStressOptions value = LprStressOptions.Parse(Array.Empty<string>());

        Assert.Equal("localhost", value.Host);
        Assert.Equal(29200, value.Port);
        Assert.Equal(9001, value.SiteId);
        Assert.Equal(2, value.Groupnum);
        Assert.Equal(9010, value.LaneId);
        Assert.Equal(new[] { 411, 412, 413, 414 }, value.DeviceNumbers);
        Assert.Equal(20, value.Count);
        Assert.Equal(50, value.IntervalMilliseconds);
        Assert.Equal(10, value.TimeoutSeconds);
    }

    [Fact]
    public void 명시한_인수를_모두_읽는다()
    {
        LprStressOptions value = LprStressOptions.Parse(new[]
        {
            "--host", "10.0.0.3", "--port", "30000", "--site", "1",
            "--group", "3", "--lane", "30", "--devices", "11,12",
            "--count", "7", "--interval-ms", "25", "--timeout-seconds", "4"
        });

        Assert.Equal("10.0.0.3", value.Host);
        Assert.Equal(30000, value.Port);
        Assert.Equal(new[] { 11, 12 }, value.DeviceNumbers);
        Assert.Equal(7, value.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("1,1")]
    [InlineData("1,2,3,4,5")]
    public void 잘못된_장비목록을_거부한다(string devices)
    {
        Assert.Throws<ArgumentException>(() =>
            LprStressOptions.Parse(new[] { "--devices", devices }));
    }

    [Theory]
    [InlineData("--count", "0")]
    [InlineData("--timeout-seconds", "0")]
    [InlineData("--port", "65536")]
    public void 잘못된_숫자범위를_거부한다(string name, string value)
    {
        Assert.Throws<ArgumentException>(() =>
            LprStressOptions.Parse(new[] { name, value }));
    }
}
