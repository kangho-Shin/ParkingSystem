using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.HelpClass
{
    public class CAMINFO
    {
        public bool use { get; set; } = false;
        public string? camip  { get; set; } = string.Empty;
        public int laneid { get; set; }
        public int devicenum { get; set; }
        public string direction { get; set; } = "Entry";
        public bool bracketmode { get; set; } = false;
        public int bracketcnt { get; set; } = 2;
        public int triggermode { get; set; } = 1;
        public int triggerpolarity { get; set; } = 0;
        public int flashmode { get; set; } = 1;
        public int flashpolarity { get; set; } = 1;
        public int exposuretime { get; set; } = 1000;
        public int framerate { get; set; } = 30;
        public int gain { get; set; } = 1;
        public int jpegq { get; set; } = 10;
        public int daytargetlum { get; set; } = 90;
        public int nighttargetlum { get; set; } = 120;
        public bool autoaec { get; set; } = true;
        public int aecmin { get; set; } = 80;
        public int aecmax { get; set; } = 2500;
        public bool autoagc { get; set; } = true;
        public int agcmin { get; set; } = 1;
        public int agcmax { get; set; } = 4;
        public string? lprname  { get; set; } = string.Empty;
        public bool schedule { get; set; } = true;
        public int presettime { get; set; } = 3600;
        public int alcstartx { get; set; } = 100;
        public int alcwidth { get; set; } = 1500;
        public int alcstarty { get; set; } = 200;
        public int alcheight { get; set; } = 1100;
        public int TriggerWidth { get; set; } = 2;
        public int TriggerInactiveWidth { get; set; } = 0;
        public int IrisVal { get; set; } = 9;
    }

    public class ExpInfo
    {
        public int SHour { get; set; }=8;
        public int SMin { get; set; } = 0;
        public int STick { get; set; }

        public int EHour { get; set; } = 18;
        public int EMin { get; set; } = 0;
        public int ETick { get; set; }

        public int ExpDayMin { get; set; } = 80;
        public int ExpDayMax { get; set; } = 1500;

        public int ExpNightMin { get; set; } = 100;
        public int ExpNightMax { get; set; } = 3500;
    }

    public class ExpSchedule
    {
        public ExpInfo[] ExpVal { get; set; } = new ExpInfo[13];

        public ExpSchedule()
        {
            for (int i = 0; i < ExpVal.Length; i++) {
                ExpVal[i] = new ExpInfo();
            }
        }
    }
}
