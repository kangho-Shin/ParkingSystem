using Parking.Contracts;

namespace Parking.Domain.Tests;

public class ContractTests
{
    [Fact]
    public void FieldEventRequest_필수식별정보를_보관한다()
    {
        Guid eventId = Guid.NewGuid();
        DateTimeOffset inDateTime = new(2026, 9, 18, 0, 0, 0, TimeSpan.Zero);

        FieldEventRequest request = new(
            eventId,
            1,
            10,
            101,
            "12가3456",
            inDateTime);

        Assert.Equal(eventId, request.EventId);
        Assert.Equal(1, request.SiteId);
        Assert.Equal(10, request.LaneId);
        Assert.Equal(101, request.DeviceId);
        Assert.Equal("12가3456", request.CarNumber);
        Assert.Equal(inDateTime, request.InDateTime);
    }

    [Fact]
    public void 차량이미지명은_현장_그룹_차로를_3자리로_만든다()
    {
        Guid eventId = Guid.Parse("4a912811-cb4d-4c3f-a70a-fe60504c3ef7");
        DateTimeOffset inDateTime = new(
            2026, 9, 19, 15, 30, 25, 123, TimeSpan.FromHours(9));

        string result = VehicleImageName.Create(
            1,
            2,
            10,
            ParkingEventType.Entry,
            inDateTime,
            "12가3456",
            eventId);

        Assert.Equal(
            "001_002_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa70afe60504c3ef7.jpg",
            result);
    }

    [Fact]
    public void 차량이미지명은_번호를_자르지_않고_금지문자를_제거한다()
    {
        string result = VehicleImageName.Create(
            1000,
            2000,
            3000,
            ParkingEventType.Exit,
            new DateTimeOffset(2026, 9, 19, 15, 30, 25, TimeSpan.Zero),
            "12가/34:56",
            Guid.Empty);

        Assert.StartsWith("1000_2000_3000_Exit_", result);
        Assert.Contains("_12가3456_", result);
    }

    [Theory]
    [InlineData(@"C:\ParkingSystem\Images\IN.jpg", "IN.jpg")]
    [InlineData("/var/parking/images/OUT.jpg", "OUT.jpg")]
    [InlineData("ONLY.jpg", "ONLY.jpg")]
    public void 이미지경로는_DB저장용_파일명으로_변환한다(string image, string expected)
    {
        Assert.Equal(expected, VehicleImageName.FileNameOnly(image));
    }

    [Fact]
    public void 입출차요청은_그룹_방향_InImage_OutImage를_보관한다()
    {
        const string inImage = @"C:\ParkingSystem\Images\001_002_010_Entry_test.jpg";
        const string outImage = @"C:\ParkingSystem\Images\001_002_020_Exit_test.jpg";
        DateTimeOffset inDateTime = DateTimeOffset.UtcNow;
        DateTimeOffset outDateTime = inDateTime.AddHours(1);

        FieldEventRequest entry = new(
            Guid.NewGuid(), 1, 10, 101, "12가3456", inDateTime,
            2, ParkingEventType.Entry, inImage);
        ExitEventRequest exit = new(
            Guid.NewGuid(), 1, 20, 201, "12가3456", outDateTime,
            2, 1, null, ParkingEventType.Exit, outImage);

        Assert.Equal(inDateTime, entry.InDateTime);
        Assert.Equal(2, entry.Groupnum);
        Assert.Equal(ParkingEventType.Entry, entry.EventType);
        Assert.Equal(inImage, entry.InImage);
        Assert.Equal(outDateTime, exit.OutDateTime);
        Assert.Equal(ParkingEventType.Exit, exit.EventType);
        Assert.Equal(outImage, exit.OutImage);
    }
}
