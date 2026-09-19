using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LprFileNameParserTests
{
    private readonly LprFileNameParser _parser = new();

    [Fact]
    public void 정상파일명에서_모든인식값을_읽는다()
    {
        LprParseResult result = _parser.Parse(
            "001_002_101_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg");

        Assert.True(result.Success);
        Assert.Equal(1, result.Recognition!.SiteId);
        Assert.Equal(2, result.Recognition.Groupnum);
        Assert.Equal(101, result.Recognition.DeviceId);
        Assert.Equal(10, result.Recognition.LaneId);
        Assert.Equal(ParkingEventType.Entry, result.Recognition.Direction);
        Assert.Equal("12가3456", result.Recognition.CarNumber);
        Assert.Equal(
            TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 9, 19, 15, 30, 25, 123)),
            result.Recognition.RecognizedAt.Offset);
    }

    [Theory]
    [InlineData("01_002_101_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg")]
    [InlineData("001_002_101_010_In_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg")]
    [InlineData("001_002_101_010_Entry_20260230153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg")]
    [InlineData("001_002_101_010_Entry_20260919153025123__4a912811cb4d4c3fa934de51805a52d1.jpg")]
    [InlineData("001_002_101_010_Entry_20260919153025123_12가3456_4a912811-cb4d-4c3f-a934-de51805a52d1.jpg")]
    [InlineData("001_002_101_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.png")]
    public void 잘못된파일명은_거부한다(string fileName)
    {
        LprParseResult result = _parser.Parse(fileName);

        Assert.False(result.Success);
        Assert.Equal("INVALID_FILE_NAME", result.ErrorCode);
    }
}
