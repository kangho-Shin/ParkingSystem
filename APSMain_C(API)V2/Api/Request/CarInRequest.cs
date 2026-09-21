using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class CarInRequest : BaseRequest
    {
        public string Carnum { get; set; } = string.Empty;
        public string Iotime { get; set; } = string.Empty;
        public string? Inimage { get; set; }
        public short? Parkcartype { get; set; }
    }
}
