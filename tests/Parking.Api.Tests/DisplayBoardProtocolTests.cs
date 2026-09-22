using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class DisplayBoardProtocolTests
{
    [Fact]
    public void 차단기열림은_기존_LDM_패킷을_사용한다()
    {
        Assert.Equal(
            new byte[] { 0x02, 0xFF, 0xC1, 0x30, 0x35, 0x39, 0x03 },
            DisplayBoardProtocol.GateOpen.ToArray());
    }

    [Fact]
    public void 두줄표시는_STX_ETX와_XOR체크섬을_생성한다()
    {
        byte[] packet = DisplayBoardProtocol.CreateTwoLine("12가3456", "입차되었습니다.");

        Assert.Equal(0x02, packet[0]);
        Assert.Equal((byte)'I', packet[3]);
        Assert.Equal((byte)(0x30 + 11), packet[4]);
        Assert.Equal(0x10, packet[9]);
        Assert.Equal((byte)'W', packet[10]);
        int secondLineIndex = Array.IndexOf(packet, (byte)0xFF, 11) + 3;
        Assert.Equal(0x10, packet[secondLineIndex]);
        Assert.Equal((byte)'W', packet[secondLineIndex + 1]);
        Assert.Equal(0x03, packet[^1]);
        byte checksum = 0;
        for (int index = 0; index < packet.Length - 2; index++)
            checksum ^= packet[index];
        Assert.Equal(checksum, packet[^2]);
    }

    [Theory]
    [InlineData(9, 5, "AM     09:05")]
    [InlineData(13, 40, "PM     13:40")]
    public void 시계는_두번째줄에_AM_PM과_현재시간을_표시한다(
        int hour, int minute, string expected)
    {
        byte[] packet = DisplayBoardProtocol.CreateClockLine(
            new DateTime(2026, 9, 22, hour, minute, 0));
        byte[] text = System.Text.Encoding.ASCII.GetBytes(expected);

        Assert.Equal(0x02, packet[0]);
        Assert.Equal((byte)'2', packet[7]);
        Assert.Equal((byte)'0', packet[8]);
        Assert.Equal(0x10, packet[9]);
        Assert.Equal((byte)'Y', packet[10]);
        Assert.True(packet.AsSpan(11, text.Length).SequenceEqual(text));
        Assert.Equal(0x03, packet[^1]);
    }

    [Fact]
    public void 다른문구_표시중에는_시계를_중지하고_11초후_즉시_재개한다()
    {
        DisplayBoardClockState state = new();
        DateTimeOffset now = new(2026, 9, 22, 13, 40, 0, TimeSpan.FromHours(9));

        Assert.True(state.ShouldSend(5001, now));
        Assert.False(state.ShouldSend(5001, now.AddSeconds(1)));

        state.Suppress(5001, now, TimeSpan.FromSeconds(11));

        Assert.False(state.ShouldSend(5001, now.AddSeconds(10)));
        Assert.True(state.ShouldSend(5001, now.AddSeconds(11)));
    }
}
