using System.Text;

namespace Parking.LprStressTester;

public sealed record LprReply(bool Acknowledged, Guid? EventId, string? ErrorCode, bool Valid);

public static class LprWireProtocol
{
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    private static readonly Encoding KoreanEncoding;

    static LprWireProtocol()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        KoreanEncoding = Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    public static byte[] EncodeRequest(string fileName)
    {
        byte[] body = KoreanEncoding.GetBytes(fileName);
        byte[] packet = new byte[body.Length + 2];
        packet[0] = Stx;
        Buffer.BlockCopy(body, 0, packet, 1, body.Length);
        packet[^1] = Etx;
        return packet;
    }
}

public sealed class LprReplyParser
{
    private readonly List<byte> _frame = new();
    private bool _receiving;

    public IReadOnlyList<LprReply> Append(ReadOnlySpan<byte> bytes)
    {
        List<LprReply> replies = new();
        foreach (byte value in bytes)
        {
            if (value == LprWireProtocol.Stx)
            {
                _frame.Clear();
                _receiving = true;
                continue;
            }
            if (!_receiving) continue;
            if (value == LprWireProtocol.Etx)
            {
                replies.Add(Parse(_frame));
                _frame.Clear();
                _receiving = false;
                continue;
            }
            if (_frame.Count >= 4096)
            {
                replies.Add(new(false, null, "RESPONSE_TOO_LONG", false));
                _frame.Clear();
                _receiving = false;
                continue;
            }
            _frame.Add(value);
        }
        return replies;
    }

    private static LprReply Parse(IReadOnlyCollection<byte> bytes)
    {
        string[] fields = Encoding.ASCII.GetString(bytes.ToArray()).Split('|');
        if (fields.Length == 2 && fields[0] == "ACK" && Guid.TryParseExact(fields[1], "N", out Guid ackId))
            return new(true, ackId, null, true);
        if (fields.Length == 3 && fields[0] == "NAK" &&
            (string.IsNullOrEmpty(fields[1]) || Guid.TryParseExact(fields[1], "N", out _)) &&
            !string.IsNullOrWhiteSpace(fields[2]))
        {
            Guid? eventId = Guid.TryParseExact(fields[1], "N", out Guid nakId) ? nakId : null;
            return new(false, eventId, fields[2], true);
        }
        return new(false, null, "INVALID_RESPONSE", false);
    }
}
