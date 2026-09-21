using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class MemberRequest : BaseRequest
    {
        public string Carnum { get; set; } = string.Empty;
    }
}
