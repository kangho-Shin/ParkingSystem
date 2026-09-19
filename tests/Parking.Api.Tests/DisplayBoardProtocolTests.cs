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
        Assert.Equal(0x03, packet[^1]);
        byte checksum = 0;
        for (int index = 0; index < packet.Length - 2; index++)
            checksum ^= packet[index];
        Assert.Equal(checksum, packet[^2]);
    }
}
