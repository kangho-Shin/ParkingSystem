using APSMain.DbModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class ParkFeeCalculator
    {
        public DateTime _InTime { get; set; }
        public DateTime _OutTime { get; set; }

        public List<DisInfo>? DisKeys;

        public int _CarType { get; set; }
        // 내부 상태
        public int totalFee = 0;         // 총 주차 요금
        public int prePay = 0;           // 사전정산한 금액
        public int totalRemainFee = 0;   // 총 할인 금액
        public int totalDiscountFee = 0; // 할인 금액
        public int remainingMinutes = 0;
        public int disTimeMinute = 0;
        public int totalTimeMinute = 0;
        public List<ParkingTime>? ptList;
        public ClsLog? XLogClass;

        public ParkFeeCalculator()
        {
            XLogClass = ClsLog.Instance;
        }

        public void InitCalculator(DateTime inTime, DateTime outTime, int carType, int prepay)
        {
            _CarType = carType;
            _InTime = inTime;
            _OutTime = outTime;
            totalFee = 0;
            totalRemainFee = 0;
            totalDiscountFee = 0;
            remainingMinutes = 0;
            disTimeMinute = 0;
            totalTimeMinute = 0;
            prePay = prepay;

            Console.WriteLine($"입차시간 : {_InTime.ToString("yyyy-MM-dd HH:mm:ss")}");
            Console.WriteLine($"정산시간 : {_OutTime.ToString("yyyy-MM-dd HH:mm:ss")}");

            XLogClass?.SaveLogString("CAL", $"입차시간 : {_InTime.ToString("yyyy-MM-dd HH:mm:ss")}");
            XLogClass?.SaveLogString("CAL", $"정산시간 : {_OutTime.ToString("yyyy-MM-dd HH:mm:ss")}");
            ptList?.Clear();

            DisKeys = ParkCache.DisKeys;
            DisKeys.Clear();

            ptList = CalculateParkingTime(inTime, outTime);

            totalFee = totalRemainFee = Calculate(ptList);

            totalTimeMinute = ptList.Sum(p => p.dayMinutes + p.nightMinutes);

            Console.WriteLine($"주차시간 : {totalTimeMinute / 60:D2}:{totalTimeMinute % 60:D2} 분");
            Console.WriteLine($"주차요금 : {totalFee:N0} 원");

            XLogClass?.SaveLogString("CAL", $"주차시간 : {totalTimeMinute / 60:D2}:{totalTimeMinute % 60:D2} 분");
            XLogClass?.SaveLogString("CAL", $"주차요금 : {totalFee:N0} 원");
        }

        public void ResetCalculator(int carType)
        {
            _CarType = carType;
            totalFee = 0;
            totalRemainFee = 0;
            totalDiscountFee = 0;
            remainingMinutes = 0;
            disTimeMinute = 0;
            totalTimeMinute = 0;

            ptList = CalculateParkingTime(_InTime, _OutTime);
            totalFee = totalRemainFee = Calculate(ptList);
        }

        public void AddDiskey(int idiskey, int isave, int ilimit)
        {
            Tdiscounttable? tdis = APSConfig.Discounts.Where(x => x.Salecode == idiskey).FirstOrDefault();

            if (tdis == null)
                return;
            ilimit = tdis.Limitval ?? 0;

            // 1. 퍼센트 할인(saletype == 4) → 무조건 1개만
            if (tdis.Saletype == 4) {
                int newPercent = tdis.Salevalue ?? 0;

                DisInfo? oldDis = DisKeys!.Where(x => {
                    Tdiscounttable? oldTdis = APSConfig.Discounts
                   .Where(d => d.Salecode == x.diskey)
                   .FirstOrDefault();

                    return oldTdis != null && oldTdis.Saletype == 4;
                }).FirstOrDefault();

                if (oldDis != null) {
                    Tdiscounttable? oldTdis = APSConfig.Discounts.Where(d => d.Salecode == oldDis.diskey)
                                                                 .FirstOrDefault();

                    int oldPercent = oldTdis?.Salevalue ?? 0;
                    if (newPercent <= oldPercent)
                        return;

                    DisKeys!.Remove(oldDis);
                }
            }
            // 2. 그 외 할인 → limit 만큼 허용
            else {
                int count = DisKeys!.Count(x => x.diskey == idiskey);

                if (ilimit > 0 && count >= ilimit)
                    return;
            }

            string stitle = tdis.Saletitle ?? "";

            DisKeys!.Add(new DisInfo
            {
                diskey = idiskey,
                save = isave,
                limit = ilimit,
                title = stitle
            });
            Console.WriteLine($"할인권등록 : key={idiskey}, count={DisKeys.Count}, title={stitle}");
        }

        public int CalculateTotalFee(DateTime inTime, DateTime outTime)
        {
            DateTime effective_InTime = inTime;
            DateTime effective_OutTime = outTime;

            disTimeMinute = 0;

            if (DisKeys != null) {
                XLogClass?.SaveLogString("CAL", $"할인키값 : COUNT={DisKeys.Count}, KEYS={string.Join(",", DisKeys.Select(x => x.diskey))}");
                Console.WriteLine($"할인키값 : COUNT={DisKeys.Count}, KEYS={string.Join(",", DisKeys.Select(x => x.diskey))}");
            }
            // 1. 시간 할인 적용
            var Discounts = DisKeys!
                            .Select(k => APSConfig.Discounts.FirstOrDefault(d => d.Salecode == k.diskey && d.Saletype == (int)DiscountType.TimeMinute))
                            .Where(d => d != null)
                            .ToList();

            foreach (var discount in Discounts) {
                if (discount?.Saletype == (int)DiscountType.TimeMinute)
                    disTimeMinute += discount.Salevalue ?? 0;
            }

            if (disTimeMinute > 0) {
                Console.WriteLine($"할인시간 : {disTimeMinute} 분");
                XLogClass?.SaveLogString("CAL", $"할인시간 : {disTimeMinute} 분");
            }
            if (APSConfig.TimeDiscountApplyType == 1) {
                effective_InTime = _InTime.AddMinutes(disTimeMinute);
            }
            else if (APSConfig.TimeDiscountApplyType == 2) {
                effective_OutTime = _OutTime.AddMinutes(-disTimeMinute);
            }
            if (effective_InTime >= effective_OutTime) {    // 무료 주차
                totalRemainFee = 0;
                totalDiscountFee = totalFee - (totalRemainFee + prePay);

                Console.WriteLine($"주차요금 : {totalFee:N0}   할인요금 : {totalDiscountFee:N0}   결재금액 : {totalRemainFee:N0}");
                XLogClass?.SaveLogString("CAL", $"주차요금 : {totalFee:N0}   할인요금 : {totalDiscountFee:N0}   결재금액 : {totalRemainFee:N0}");
                return totalRemainFee;
            }

            // 주차 시간 계산
            ptList?.Clear();
            ptList = CalculateParkingTime(effective_InTime, effective_OutTime);

            totalTimeMinute = ptList.Sum(p => p.dayMinutes + p.nightMinutes);

            if (totalTimeMinute < APSConfig.GraceTime) {
                totalRemainFee = 0;
                totalFee = 0;
                totalDiscountFee = 0;
                XLogClass?.SaveLogString("CAL", $"회차차량");
                Console.WriteLine($"회차차량");
                return totalFee;
            }

            // 3. 요금 계산 (스텝 적용 + 일일 최대요금)
            totalRemainFee = Calculate(ptList);

            // 2. 금액 할인 적용
            Discounts = DisKeys!.Select(k => APSConfig.Discounts.FirstOrDefault(d => d.Salecode == k.diskey && d.Saletype == (int)DiscountType.Amount))
                               .Where(d => d != null)
                               .ToList();

            foreach (var k in Discounts) {
                int mvalue = k?.Salevalue ?? 0;
                XLogClass?.SaveLogString("CAL", $"금액할인 :{totalRemainFee:N0}   할인:{mvalue:N0}");
                Console.WriteLine($"금액할인 :{totalRemainFee:N0}   할인:{mvalue:N0}");
                totalRemainFee -= mvalue;
            }

            if (totalRemainFee > 0) {
                // 4. 퍼센트 할인 적용
                totalRemainFee = ApplyPercentDiscounts();
            }



            totalRemainFee = totalRemainFee - prePay;
            if (totalRemainFee < 0) { totalRemainFee = 0; }
            totalDiscountFee = totalFee - (totalRemainFee + prePay);

            if (_CarType == 3) {
                totalRemainFee += APSConfig.ViolationFee;
                totalFee += APSConfig.ViolationFee;
            }

            Console.WriteLine($"주차요금 : {totalFee:N0}   할인요금 : {totalDiscountFee:N0}   결재금액 : {totalRemainFee:N0}");
            XLogClass?.SaveLogString("CAL", $"주차요금 : {totalFee:N0}   할인요금 : {totalDiscountFee:N0}   결재금액 : {totalRemainFee:N0}");

            return totalRemainFee;
        }

        private int ApplyPercentDiscounts()
        {
            var discounts = DisKeys!
                           .Select(k => APSConfig.Discounts.FirstOrDefault(d => d.Salecode == k.diskey && d.Saletype == (int)DiscountType.Percent))
                           .Where(d => d != null && d.Salevalue > 0)
                           .ToList();

            if (discounts.Count <= 0)
                return totalRemainFee;

            double discountedFee = totalRemainFee;

            foreach (var discount in discounts) {
                double percent = (discount!.Salevalue ?? 0) / 100.0;
                discountedFee *= 1.0 - percent;
            }

            XLogClass?.SaveLogString("CAL", $"퍼센트할인 : {totalRemainFee:N0}      할인 : {discountedFee:N0}");
            Console.WriteLine($"퍼센트할인 : {totalRemainFee:N0}      할인 : {discountedFee:N0}");
            return Math.Max(((int)discountedFee / 10) * 10, 0);
        }

        private List<ParkingTime> CalculateParkingTime(DateTime inTime, DateTime outTime)
        {
            var result = new List<ParkingTime>();
            DateTime currentDate = inTime.Date;

            while (currentDate <= outTime.Date) {
                DateTime start = currentDate == inTime.Date ? inTime : currentDate;
                DateTime end = currentDate == outTime.Date ? outTime : currentDate.AddDays(1);

                int dayMinutes = 0;
                int nightMinutes = 0;

                bool isHoliday = APSConfig.Holidays.Any(h => h.Hdate.Date == currentDate.Date);
                bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;

                if ((APSConfig.ExcludeHoliday == 1 && isHoliday) ||
                    (APSConfig.ExcludeWeekend == 1 && isWeekend)) {
                    result.Add(new ParkingTime
                    {
                        Date = currentDate,
                        dayMinutes = 0,
                        nightMinutes = 0
                    });

                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                int totalMinutes = (int)(end - start).TotalMinutes;
                if (totalMinutes < 0)
                    totalMinutes = 0;

                dayMinutes = GetDayMinutes(currentDate, start, end);
                if (dayMinutes > totalMinutes)
                    dayMinutes = totalMinutes;

                nightMinutes = totalMinutes - dayMinutes;
                if (nightMinutes < 0)
                    nightMinutes = 0;

                result.Add(new ParkingTime
                {
                    Date = currentDate,
                    dayMinutes = dayMinutes,
                    nightMinutes = nightMinutes
                });

                currentDate = currentDate.AddDays(1);
            }

            return result;
        }

        public static bool IsDayTime(DateTime time)
        {
            if (!APSConfig.DayTimeRanges.TryGetValue(time.DayOfWeek, out var range)) {
                Console.WriteLine($"DayTimeRanges miss : {time.DayOfWeek}");
                return true;
            }

            TimeSpan t = time.TimeOfDay;
            if (range.Start == range.End)
                return false;

            if (range.Start < range.End)
                return t >= range.Start && t < range.End;

            return t >= range.Start || t < range.End;
        }

        private int Calculate(List<ParkingTime> ptList)
        {
            int parkFee = 0;

            foreach (var pt in ptList) {
                int fee = 0;

                bool isWeekend = pt.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                int weekType = isWeekend ? 2 : 1;

                fee += CalculateShiftFee(pt.Date, weekType, 1, pt.dayMinutes);   // 오전
                fee += CalculateShiftFee(pt.Date, weekType, 2, pt.nightMinutes); // 오후

                if (APSConfig.MaxDailyFee > 0)
                    fee = Math.Min(fee, APSConfig.MaxDailyFee);

                pt.Fee = fee;
                parkFee += fee;

                Console.WriteLine($"{pt.Date:yyyy-MM-dd} : 오전주차:{pt.dayMinutes,5}    오후주차:{pt.nightMinutes,5}    주차요금:{pt.Fee,5}");
            }

            Console.WriteLine("==============================================================");
            return parkFee;
        }

        private int CalculateShiftFee(DateTime date, int weekType, int dayShift, int remainMinutes)
        {
            int fee = 0;

            if (remainMinutes <= 0)
                return 0;

            var matchedSteps = APSConfig.FeeRules
                .Where(r => r.Cartype == _CarType &&
                            r.Weektype == weekType &&
                            r.Dayshift == dayShift &&
                            r.Parktime.HasValue &&
                            r.Parktime.Value > 0)
                .OrderBy(r => r.Feestep)
                .Select(r => new ParkFeeStep
                {
                    Feestep = r.Feestep ?? 0,
                    DayShift = r.Dayshift ?? 0,
                    UnitMinutes = r.Parktime ?? 0,
                    FeePerUnit = r.Parkfee ?? 0,
                    MaxCount = r.Maxcount ?? 0
                })
                .ToList();


            foreach (var step in matchedSteps) {
                //                Console.WriteLine($"weekType:{weekType}, Feestep:{step.Feestep}, dayShift:{dayShift}, UnitMinutes:{step.UnitMinutes}, FeePerUnit:{step.FeePerUnit}, MaxCount:{step.MaxCount}");
                if (remainMinutes <= 0)
                    break;

                if (step.UnitMinutes <= 0)
                    continue;

                int count;
                if (step.MaxCount == 0) {
                    count = (remainMinutes + step.UnitMinutes - 1) / step.UnitMinutes;
                }
                else {
                    count = Math.Min((remainMinutes + step.UnitMinutes - 1) / step.UnitMinutes, step.MaxCount);
                }

                if (count <= 0)
                    continue;

                int billMinutes = count * step.UnitMinutes;
                int usedMinutes = Math.Min(remainMinutes, billMinutes);
                int stepFee = count * step.FeePerUnit;

                fee += stepFee;
                remainMinutes -= usedMinutes;

                //Console.WriteLine($"dayShift({dayShift}-{count}) 남은시간:{remainMinutes,5} 적용시간:{usedMinutes,5} 청구단위시간:{billMinutes,5} step요금:{stepFee,5} 누적요금:{fee,5}");
            }

            return fee;
        }

        private int GetDayMinutes(DateTime baseDate, DateTime start, DateTime end)
        {
            if (!APSConfig.DayTimeRanges.TryGetValue(baseDate.DayOfWeek, out var range)) {
                //               Console.WriteLine($"DayTimeRanges miss : {baseDate.DayOfWeek}");
                return (int)(end - start).TotalMinutes;
            }

            if (range.Start == range.End)
                return 0;

            DateTime dayStart1;
            DateTime dayEnd1;
            int minutes = 0;

            if (range.Start < range.End) {
                dayStart1 = baseDate.Date.Add(range.Start);
                dayEnd1 = baseDate.Date.Add(range.End);
                minutes += GetOverlapMinutes(start, end, dayStart1, dayEnd1);
            }
            else {
                DateTime dayStartEarly = baseDate.Date;
                DateTime dayEndEarly = baseDate.Date.Add(range.End);

                DateTime dayStartLate = baseDate.Date.Add(range.Start);
                DateTime dayEndLate = baseDate.Date.AddDays(1);

                minutes += GetOverlapMinutes(start, end, dayStartEarly, dayEndEarly);
                minutes += GetOverlapMinutes(start, end, dayStartLate, dayEndLate);
            }

            return minutes;
        }

        private int GetOverlapMinutes(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            DateTime maxStart = start1 > start2 ? start1 : start2;
            DateTime minEnd = end1 < end2 ? end1 : end2;

            if (minEnd <= maxStart)
                return 0;

            return (int)(minEnd - maxStart).TotalMinutes;
        }

        private int ReduceMinutes(ParkFeeStep step, ref int remainingMinutes)
        {
            int fee = 0;
            int unit = step.UnitMinutes;
            int maxCount = step.MaxCount;

            while (remainingMinutes >= unit) {
                fee += step.FeePerUnit;
                remainingMinutes -= unit;

                if (maxCount > 0) {
                    maxCount--;
                    if (maxCount == 0)
                        break;
                }
            }

            return fee;
        }

        public void RemoveDisKey(int keytype)
        {
            DisKeys!.RemoveAll(x => x.save == keytype);
        }
    }
}
