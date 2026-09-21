using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class DayTimeRange
    {
        public int WeekType { get; set; }      // 0:일, 1:월, 2:화, 3:수, 4:목, 5:금, 6:토
        public int SHour { get; set; }         // 분 단위
        public int EHour { get; set; }         // 분 단위
    }

    public static class DayShiftHelper
    {
        public static int GetWeekType(DateTime dateTime)
        {
            return dateTime.DayOfWeek switch
            {
                DayOfWeek.Sunday => 0,
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => 1
            };
        }

        public static int GetMinuteOfDay(DateTime dateTime)
        {
            return (dateTime.Hour * 60) + dateTime.Minute;
        }

        public static int GetDayShift(DateTime dateTime, List<DayTimeRange> dayTimeRanges)
        {
            int weekType = GetWeekType(dateTime);
            int minuteOfDay = GetMinuteOfDay(dateTime);

            DayTimeRange? range = dayTimeRanges.FirstOrDefault(x => x.WeekType == weekType);
            if (range == null)
                return 0;

            if (range.SHour <= range.EHour) {
                return (minuteOfDay >= range.SHour && minuteOfDay < range.EHour) ? 0 : 1;
            }
            else {
                return (minuteOfDay >= range.SHour || minuteOfDay < range.EHour) ? 0 : 1;
            }
        }
    }
}
