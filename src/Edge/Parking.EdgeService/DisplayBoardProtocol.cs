using System.Text;

namespace Parking.EdgeService;

public static class DisplayBoardProtocol
{
    public static ReadOnlySpan<byte> GateOpen =>
        new byte[] { 0x02, 0xFF, 0xC1, 0x30, 0x35, 0x39, 0x03 };

    public static byte[] CreateTwoLine(string firstLine, string secondLine)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        List<byte> packet = new(128)
        {
            0x02, 0xFF, 0x1B, (byte)'I', (byte)(0x30 + 11), (byte)'M', (byte)'0', (byte)'1',
            ScrollFlag(firstLine), 0x10, (byte)'W'
        };
        packet.AddRange(encoding.GetBytes(firstLine));
        packet.Add(0xFF);
        packet.Add((byte)'2');
        packet.Add(ScrollFlag(secondLine));
        packet.Add(0x10);
        packet.Add((byte)'W');
        packet.AddRange(encoding.GetBytes(secondLine));
        packet.Add(0xFF);
        packet.Add(0x00);
        packet.Add(0x03);

        byte checksum = 0;
        for (int index = 0; index < packet.Count - 2; index++)
        {
            if (packet[index] == 0x79)
                packet[index] = 0xFF;
            checksum ^= packet[index];
        }
        packet[^2] = checksum;
        return packet.ToArray();
    }

    private static byte ScrollFlag(string text) =>
        GetDisplayLength(text) > 12 ? (byte)'1' : (byte)'0';

    private static int GetDisplayLength(string text) =>
        text.Sum(character => character > 127 ? 2 : 1);
}
