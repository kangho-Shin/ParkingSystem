using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class CarOutRequest : BaseRequest
    {
        public string Carnum { get; set; } = string.Empty;
        public int CarGubun { get; set; } = 1;
        public string? Iotime { get; set; }
        public string? Outimage { get; set; }
    }
}
