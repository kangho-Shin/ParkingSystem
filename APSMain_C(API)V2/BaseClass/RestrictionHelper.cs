using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public static class RestrictionHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="carNum"></param>
        /// <param name="restriction 0:사용안함 1:2부제 2:5부제(날짜) 3:10부제 4:요일제"></param>
        /// <returns>true:통과차량 false:거부차량</returns>
        public static bool RestrictionDayOfWeek(string carNum, int restriction)
        {
            DateTime now = DateTime.Now;
            int dayOfWeek = (int)now.DayOfWeek;   // 0 = Sunday

            if (string.IsNullOrWhiteSpace(carNum))
                return false;

            char lastCh = carNum[carNum.Length - 1];
            if (!char.IsDigit(lastCh))
                return false;

            int lastNum = lastCh - '0';
            if (restriction == 1)   // 2부제
            {
                if (dayOfWeek >= 1 && dayOfWeek <= 5) {
                    if ((now.Day % 2) == (lastNum % 2)) {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            else if ( restriction == 2 )   // 5부제
            {
                if (dayOfWeek >= 1 && dayOfWeek <= 5) {
                    if ((now.Day % 5) != (lastNum % 5)) {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            else if (restriction == 3)   // 10부제
            {
                if (dayOfWeek >= 1 && dayOfWeek <= 5) {
                    if ((now.Day % 10) != (lastNum % 10)) {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            else if (restriction == 4)   // 요일제
            {
                if (dayOfWeek >= 1 && dayOfWeek <= 5) {
                    bool isRestricted = dayOfWeek switch
                    {
                        1 => lastNum == 1 || lastNum == 6,
                        2 => lastNum == 2 || lastNum == 7,
                        3 => lastNum == 3 || lastNum == 8,
                        4 => lastNum == 4 || lastNum == 9,
                        5 => lastNum == 5 || lastNum == 0,
                        _ => false
                    };

                    return !isRestricted;
                }
            }
            return true;
        }
    }
}
