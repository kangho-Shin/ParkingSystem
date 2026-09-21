using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Request
{
    public class CarIoSendData
    {
        public short Sitenum { get; set; }
        public short Groupnum { get; set; }
        public short Devicenum { get; set; }
        public string Carnum { get; set; } = "";
        public string Iotime { get; set; } = "";
        public string? Inimage { get; set; }
        public string? Outimage { get; set; }
        public string? Backimage { get; set; }
        public string? Ticketdata { get; set; }
        public int? Ticketnum { get; set; }
        public short? Ticketcartype { get; set; }
        public short? Parkcartype { get; set; }
        public int? Intick { get; set; }
        public int? Outtick { get; set; }
        public int? Cardid { get; set; }
        public string? Name { get; set; }
        public string? Cartype { get; set; }
        public DateTime? Enddate { get; set; }
        public short? Managercode { get; set; }
        public string? Managername { get; set; }
        public string? Note { get; set; }
    }
}
