namespace Parking.EdgeService;

public sealed record LprFrameResult(byte[]? Data, string? ErrorCode);

public sealed class LprFrameParser
{
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const int MaximumDataLength = 1024;

    private readonly List<byte> _data = new(MaximumDataLength);
    private bool _insideFrame;
    private bool _overflowed;

    public IReadOnlyList<LprFrameResult> Append(ReadOnlySpan<byte> bytes)
    {
        List<LprFrameResult> results = new();
        foreach (byte value in bytes)
        {
            if (value == Stx)
            {
                _insideFrame = true;
                _overflowed = false;
                _data.Clear();
                continue;
            }

            if (!_insideFrame)
                continue;

            if (value == Etx)
            {
                if (_overflowed)
                    results.Add(new LprFrameResult(null, "FRAME_TOO_LONG"));
                else if (_data.Count == 0)
                    results.Add(new LprFrameResult(null, "EMPTY_DATA"));
                else
                    results.Add(new LprFrameResult(_data.ToArray(), null));
                _insideFrame = false;
                _overflowed = false;
                _data.Clear();
                continue;
            }

            if (!_overflowed)
            {
                if (_data.Count < MaximumDataLength)
                    _data.Add(value);
                else
                {
                    _overflowed = true;
                    _data.Clear();
                }
            }
        }
        return results;
    }
}
