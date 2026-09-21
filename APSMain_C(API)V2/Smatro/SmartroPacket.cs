using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Smatro
{
    public class SmartroPacket
    {
        public string TerminalId { get; set; } = "";
        public string DateTimeText { get; set; } = "";
        public byte JobCode { get; set; }
        public byte ResponseCode { get; set; }
        public ushort BodyLength { get; set; }
        public byte[] Body { get; set; } = Array.Empty<byte>();
    }
}
