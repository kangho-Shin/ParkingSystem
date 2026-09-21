using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.BaseClass
{
    public static class JPXConfig
    {
        public static int Sitenum { get; set; } = 9001;
        public static int Groupnum { get; set; } = 2;
        public static string EdgeServiceHost { get; set; } = "127.0.0.1";
        public static int EdgeServicePort { get; set; } = 29200;
        public static int BASENUM { get; set; } = 401;
        public static bool DummyTest { get; set; } = false;
        public static bool DebugMode { get; set; } = false;
    }
}
