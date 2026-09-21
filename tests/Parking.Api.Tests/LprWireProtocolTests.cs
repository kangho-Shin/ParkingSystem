using System.Text;
using Parking.LprStressTester;

namespace Parking.Api.Tests;

public sealed class LprWireProtocolTests
{
    [Fact]
    public void 요청은_STX_CP949_ETX로_만든다()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] packet = LprWireProtocol.EncodeRequest("12가3456.jpg");

        Assert.Equal(0x02, packet[0]);
        Assert.Equal(0x03, packet[^1]);
        Assert.Equal("12가3456.jpg", Encoding.GetEncoding(949).GetString(packet[1..^1]));
    }

    [Fact]
    public void 분할된_ACK를_하나의_응답으로_조립한다()
    {
        Guid eventId = Guid.NewGuid();
        byte[] packet = Frame($"ACK|{eventId:N}");
        LprReplyParser parser = new();
        List<LprReply> replies = new();

        foreach (byte value in packet)
            replies.AddRange(parser.Append(new[] { value }));

        LprReply reply = Assert.Single(replies);
        Assert.True(reply.Valid);
        Assert.True(reply.Acknowledged);
        Assert.Equal(eventId, reply.EventId);
    }

    [Fact]
    public void 한_수신에_포함된_두_응답을_분리한다()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        byte[] packet = Frame($"ACK|{first:N}").Concat(Frame($"NAK|{second:N}|INVALID_LANE")).ToArray();

        IReadOnlyList<LprReply> replies = new LprReplyParser().Append(packet);

        Assert.Equal(2, replies.Count);
        Assert.True(replies[0].Acknowledged);
        Assert.False(replies[1].Acknowledged);
        Assert.Equal("INVALID_LANE", replies[1].ErrorCode);
    }

    [Fact]
    public void 잘못된_응답은_유효하지_않다()
    {
        LprReply reply = Assert.Single(new LprReplyParser().Append(Frame("UNKNOWN")));
        Assert.False(reply.Valid);
    }

    private static byte[] Frame(string value) =>
        new byte[] { 0x02 }.Concat(Encoding.ASCII.GetBytes(value)).Append((byte)0x03).ToArray();
}
