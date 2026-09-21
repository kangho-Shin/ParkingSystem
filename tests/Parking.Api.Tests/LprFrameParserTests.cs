using System.Text;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LprFrameParserTests
{
    [Fact]
    public void 분할수신과_합쳐진패킷을_순서대로_복원한다()
    {
        byte[] first = LprProtocol.EncodeRequest("첫번째.jpg");
        byte[] second = LprProtocol.EncodeRequest("두번째.jpg");
        LprFrameParser parser = new();

        Assert.Empty(parser.Append(first.AsSpan(0, 3)));
        byte[] remainder = first.AsSpan(3).ToArray().Concat(second).ToArray();
        IReadOnlyList<LprFrameResult> results = parser.Append(remainder);

        Assert.Equal(2, results.Count);
        Assert.Equal("첫번째.jpg", LprProtocol.Decode(results[0].Data!).Data);
        Assert.Equal("두번째.jpg", LprProtocol.Decode(results[1].Data!).Data);
    }

    [Fact]
    public void 프레임도중_STX는_이전데이터를_버린다()
    {
        LprFrameParser parser = new();
        byte[] bytes = { 0x55, 0x02, (byte)'A', 0x02, (byte)'B', 0x03 };

        LprFrameResult result = Assert.Single(parser.Append(bytes));

        Assert.Equal("B", Encoding.ASCII.GetString(result.Data!));
    }

    [Fact]
    public void 빈프레임과_길이초과는_오류를_반환한다()
    {
        LprFrameParser parser = new();
        Assert.Equal("EMPTY_DATA", Assert.Single(parser.Append(new byte[] { 0x02, 0x03 })).ErrorCode);

        byte[] oversized = new byte[1027];
        oversized[0] = 0x02;
        Array.Fill(oversized, (byte)'A', 1, 1025);
        oversized[^1] = 0x03;
        Assert.Equal("FRAME_TOO_LONG", Assert.Single(parser.Append(oversized)).ErrorCode);
    }

    [Fact]
    public void 잘못된_949바이트는_인코딩오류다()
    {
        LprDecodeResult result = LprProtocol.Decode(new byte[] { 0x81 });

        Assert.False(result.Success);
        Assert.Equal("INVALID_ENCODING", result.ErrorCode);
    }

    [Fact]
    public void ACK와_NAK는_STX_ETX와_EventId를_포함한다()
    {
        Guid eventId = Guid.NewGuid();
        byte[] ack = LprProtocol.CreateReply(true, eventId, null);
        byte[] nak = LprProtocol.CreateReply(false, eventId, "INVALID_LANE");

        Assert.Equal(0x02, ack[0]);
        Assert.Equal(0x03, ack[^1]);
        Assert.Equal($"ACK|{eventId:N}", Encoding.ASCII.GetString(ack[1..^1]));
        Assert.Equal($"NAK|{eventId:N}|INVALID_LANE", Encoding.ASCII.GetString(nak[1..^1]));
    }
}
