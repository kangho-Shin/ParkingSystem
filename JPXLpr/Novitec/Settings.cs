using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.Novitec
{
    public class LPRROISettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public LPRROISettings()
        {
            X = 0;
            Y = 0;
            Width = 1920;
            Height = 1080;
        }
    }
}
