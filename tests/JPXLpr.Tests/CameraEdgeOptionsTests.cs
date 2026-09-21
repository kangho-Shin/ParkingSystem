using JPXLpr.Edge;
using JPXLpr.BaseClass;
using JPXLpr.HelpClass;

namespace JPXLpr.Tests;

public sealed class CameraEdgeOptionsTests
{
    [Fact]
    public void Entry_camera_maps_site_group_lane_and_device()
    {
        CAMINFO camera = new() { laneid = 9010, devicenum = 401, direction = "Entry" };
        EdgeLprOptions value = CameraEdgeOptions.Create(9001, 2, "127.0.0.1", 29200, camera);
        Assert.Equal(new[] { 9001, 2, 9010, 401 },
            new[] { value.Sitenum, value.Groupnum, value.Laneid, value.Devicenum });
        Assert.Equal("Entry", value.Direction);
    }

    [Theory]
    [InlineData(0, 9010, 401, "Entry")]
    [InlineData(1, 9010, 403, "Entry")]
    [InlineData(2, 9020, 402, "Exit")]
    [InlineData(3, 9020, 404, "Exit")]
    public void Camera_defaults_follow_type_ranges(int index, int lane, int device, string direction)
    {
        CAMINFO[] values = CameraConfigFile.CreateDefaultsForTest();
        Assert.Equal((lane, device, direction),
            (values[index].laneid, values[index].devicenum, values[index].direction));
    }
}
