using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class BaseRequest
    {
        public short Sitenum { get; set; }
        public short Groupnum { get; set; }
        public short Devicenum { get; set; }
    }
}
