using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class EnvConfigRequest
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
    }

    public class EnvRequest : BaseRequest
    {
    }

    public class EnvItemRequest : BaseRequest
    {
        public string CmdType { get; set; } = string.Empty;
    }
}
