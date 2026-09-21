using System.Text;

namespace Parking.EdgeService;

public sealed record LprDecodeResult(bool Success, string? Data, string? ErrorCode);

public static class LprProtocol
{
    private static readonly Encoding KoreanEncoding;

    static LprProtocol()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        KoreanEncoding = Encoding.GetEncoding(
            949,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    public static LprDecodeResult Decode(byte[] data)
    {
        try
        {
            return new LprDecodeResult(true, KoreanEncoding.GetString(data), null);
        }
        catch (DecoderFallbackException)
        {
            return new LprDecodeResult(false, null, "INVALID_ENCODING");
        }
    }

    public static byte[] EncodeRequest(string fileName)
    {
        byte[] data = KoreanEncoding.GetBytes(fileName);
        return Frame(data);
    }

    public static byte[] CreateReply(
        bool acknowledged,
        Guid? eventId,
        string? errorCode)
    {
        string id = eventId?.ToString("N") ?? "";
        string payload = acknowledged
            ? $"ACK|{id}"
            : $"NAK|{id}|{errorCode ?? "PROCESSING_ERROR"}";
        return Frame(Encoding.ASCII.GetBytes(payload));
    }

    private static byte[] Frame(byte[] data)
    {
        byte[] packet = new byte[data.Length + 2];
        packet[0] = LprFrameParser.Stx;
        Buffer.BlockCopy(data, 0, packet, 1, data.Length);
        packet[^1] = LprFrameParser.Etx;
        return packet;
    }
}
