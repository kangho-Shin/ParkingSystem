using JPXLpr.HelpClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JPXLpr.BaseClass
{
    public static class ExpScheduleFile
    {
        private static ExpSchedule[] CreateDefault()
        {
            int[,] sunTime = {  { 0,  0,  0,  0 },
                                { 7, 24, 17, 25 },
                                { 7, 32, 17, 56 },
                                { 7, 01, 18, 25 },
                                { 6, 17, 18, 52 },
                                { 5, 37, 19, 18 },
                                { 5, 14, 19, 42 },
                                { 5, 16, 19, 52 },
                                { 5, 37, 19, 36 },
                                { 6, 02, 18, 59 },
                                { 6, 25, 18, 14 },
                                { 6, 53, 17, 34 },
                                { 7, 23, 17, 15 } };
            ExpSchedule[] schedules = new ExpSchedule[4];

            for (int cam = 0; cam < schedules.Length; cam++) {
                schedules[cam] = new ExpSchedule();

                for (int mon = 1; mon <= 12; mon++) {
                    int sTick = (sunTime[mon, 0] * 60 + sunTime[mon, 1]) - 60;
                    int eTick = (sunTime[mon, 2] * 60 + sunTime[mon, 3]) + 60;

                    schedules[cam].ExpVal[mon] = new ExpInfo
                    {
                        SHour = sTick / 60,
                        SMin = sTick % 60,
                        STick = sTick,

                        EHour = eTick / 60,
                        EMin = eTick % 60,
                        ETick = eTick,

                        ExpDayMin = 80,
                        ExpDayMax = 1500,

                        ExpNightMin = 100,
                        ExpNightMax = 2800
                    };
                }

                schedules[cam].ExpVal[0] = schedules[cam].ExpVal[1];
            }

            return schedules;
        }

        private static readonly JsonSerializerOptions _jsonOpt = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public static ExpSchedule[] Load(string fileName)
        {
            try {
                if (!File.Exists(fileName))
                    return CreateDefault();

                string json = File.ReadAllText(fileName);

                ExpSchedule[]? data =
                    JsonSerializer.Deserialize<ExpSchedule[]>(json, _jsonOpt);

                if (data == null || data.Length == 0)
                    return CreateDefault();

                return data;
            }
            catch {
                return CreateDefault();
            }
        }

        public static void Save(string fileName, ExpSchedule[] data)
        {
            string json = JsonSerializer.Serialize(data, _jsonOpt);

            File.WriteAllText(fileName, json);
        }
    }
}
