using APSMain.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class CarPayRequest : BaseRequest
    {
        public int CarGubun { get; set; }   // 1: 일반, 2: 등록

        public Tparkinfo? Tparkinfo { get; set; }
        public List<Tdiscountinfo> Tdiscountinfo { get; set; } = new();
        public List<Tbcardinfo> Tbcardinfo { get; set; } = new();

        public Tperiodmember? Tperiodmember { get; set; }
    }
}
