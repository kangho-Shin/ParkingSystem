using System.Text;

namespace JPXLpr.Edge;

public static class EdgeLprFrame
{
    static EdgeLprFrame() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static byte[] Encode(string fileName)
    {
        byte[] body = Encoding.GetEncoding(949).GetBytes(fileName);
        byte[] frame = new byte[body.Length + 2];
        frame[0] = 0x02;
        Buffer.BlockCopy(body, 0, frame, 1, body.Length);
        frame[^1] = 0x03;
        return frame;
    }
}
