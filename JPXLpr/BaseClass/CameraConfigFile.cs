using JPXLpr.HelpClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JPXLpr.BaseClass
{
    public static class CameraConfigFile
    {
        private static readonly JsonSerializerOptions _jsonOpt = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public static CAMINFO[] Load(string fileName)
        {
            if (!File.Exists(fileName))
                return CreateDefault();

            string json = File.ReadAllText(fileName);

            CAMINFO[]? camInfos = JsonSerializer.Deserialize<CAMINFO[]>(json, _jsonOpt);

            if (camInfos == null || camInfos.Length == 0)
                return CreateDefault();

            return camInfos;
        }

        public static void Save(string fileName, CAMINFO[] camInfos)
        {
            string json = JsonSerializer.Serialize(camInfos, _jsonOpt);
            File.WriteAllText(fileName, json);
        }

        private static CAMINFO[] CreateDefault()
        {
            CAMINFO[] camInfos = new CAMINFO[4];
            int[] deviceNumbers = { 401, 403, 402, 404 };

            for (int i = 0; i < camInfos.Length; i++) {
                camInfos[i] = new CAMINFO
                {
                    use = false,
                    camip = $"192.168.0.14{i+1}",
                    lprname = i < 2 ? "입구" : "출구",
                    laneid = i < 2 ? 9010 : 9020,
                    devicenum = deviceNumbers[i],
                    direction = i < 2 ? "Entry" : "Exit",
                    bracketmode = false,
                    bracketcnt = 1,
                    exposuretime = 1000,
                    framerate = 25,
                    gain = 1,
                    jpegq = 10,
                    daytargetlum = 80,
                    nighttargetlum = 120,
                    autoaec = true,
                    aecmin = 100,
                    aecmax = 1500,
                    autoagc = true,
                    agcmin = 1,
                    agcmax = 2,
                    IrisVal = 9,
                    schedule = true
                };
            }

            return camInfos;
        }

        internal static CAMINFO[] CreateDefaultsForTest() => CreateDefault();
    }
}
