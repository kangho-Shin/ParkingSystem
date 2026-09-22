using APSMain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Response
{
    public class CarInResponse
    {
        public int Result { get; set; }
        public string ResultMsg { get; set; } = string.Empty;
        public string Carnum { get; set; } = string.Empty;
        public int Xindex { get; set; }

        public Tperiodmember? PeriodMember { get; set; }
    }
}
