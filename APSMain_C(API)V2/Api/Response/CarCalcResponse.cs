using APSMain.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Response
{
    public class CarCalcItem
    {
        public Tparkinfo? Tparkinfo { get; set; }
        public Tdisperson? Tdisperson { get; set; }
        public List<Tdiscountinfo>? Tdiscountinfo { get; set; }
        public List<Tbcardinfo>? Tbcardinfo { get; set; }
    }

    public class CarCalcResponse
    {
        public string? Result { get; set; }
        public string? Message { get; set; }

        public List<CarCalcItem>? Cars { get; set; }
        public List<Tperiodmember>? PeriodMembers { get; set; }
    }
}
