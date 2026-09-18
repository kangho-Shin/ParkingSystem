namespace WebApplication_PMS
{
    public class ParkFeeCalculator
    {
        public DateTime _InTime { get; set; }
        public DateTime _OutTime { get; set; }

        public List<int> DisKeys { get; set; } = new();

        public int _CarType { get; set; }
        // 내부 상태
        public int totalFee = 0;         // 총 주차 요금
        public int totalRemainFee = 0;   // 총 할인 금액
        public int totalDiscountFee = 0; // 할인 금액
        public int remainingMinutes = 0;
        public int disTimeMinute = 0;
        public int totalTimeMinute = 0;
        public List<ParkingTime>? ptList;

        public void InitCalculator(DateTime inTime, DateTime outTime, int carType)
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

            ptList?.Clear();
            ptList = CalculateParkingTime(inTime, outTime);
            totalFee = Calculate(ptList);
        }

        public int CalculateTotalFee(DateTime inTime, DateTime outTime, int carType)
        {
            DateTime effective_InTime = inTime;
            DateTime effective_OutTime = outTime;
            _CarType = carType;

            // 1. 시간 할인 적용
            //var Discounts = APSConfig.Discounts
            //                .Where(d => DisKeys.Contains(d.Key) && d.Type == (int)DiscountType.TimeMinute)
            //                .ToList();
            var Discounts = DisKeys
                            .Select(key => APSConfig.Discounts.FirstOrDefault(d => d.Key == key && d.Type == (int)DiscountType.TimeMinute))
                            .Where(d => d != null) // FirstOrDefault는 null 가능성 있음
                            .ToList();
            foreach (var discount in Discounts)
            {
                if (discount?.Type == (int)DiscountType.TimeMinute)
                    disTimeMinute += discount.Value ?? 0;
            }

            if (APSConfig.TimeDiscountApplyType == 1)
            {
                effective_InTime = _InTime.AddMinutes(disTimeMinute);
            }
            else if (APSConfig.TimeDiscountApplyType == 2)
            {
                effective_OutTime = _OutTime.AddMinutes(-disTimeMinute);
            }
            if (effective_InTime >= effective_OutTime)    // 무료 주차
                return 0;

            // 2. 주차 시간 계산
            ptList?.Clear();
            ptList = CalculateParkingTime(effective_InTime, effective_OutTime);

            // 3. 요금 계산 (스텝 적용 + 일일 최대요금)
            Calculate(ptList);

            // 4. 퍼센트 할인 적용
            totalRemainFee = ApplyPercentDiscounts(ptList);

            totalTimeMinute = ptList.Sum(p => p.dayMinutes + p.nightMinutes);

            totalDiscountFee = totalFee - totalRemainFee;
            return totalRemainFee;
        }

        private int ApplyPercentDiscounts(List<ParkingTime> ptList)
        {
            int baseFee = ptList.Sum(p => p.Fee);
            double discountedFee = baseFee;

            //var Discounts = APSConfig.Discounts
            //                .Where(d => DisKeys.Contains(d.Key) && d.Type == (int)DiscountType.Percent)
            //                .ToList();
            var Discounts = DisKeys
                            .Select(key => APSConfig.Discounts.FirstOrDefault(d => d.Key == key && d.Type == (int)DiscountType.Percent))
                            .Where(d => d != null) // FirstOrDefault는 null 가능성 있음
                            .ToList();
            foreach (var discount in Discounts)
            {
                if (discount?.Value > 0)
                {
                    double percent = ((int)discount.Value / 100.0); // 20 → 0.2
                    discountedFee *= (1.0 - percent);
                }
            }

            return Math.Max((int)Math.Round(discountedFee), 0);
        }

        private List<ParkingTime> CalculateParkingTime(DateTime inTime, DateTime outTime)
        {
            var result = new List<ParkingTime>();
            DateTime currentDate = inTime.Date;

            while (currentDate <= outTime.Date)
            {
                DateTime start = currentDate == inTime.Date ? inTime : currentDate;
                DateTime end = currentDate == outTime.Date ? outTime : currentDate.AddDays(1);

                TimeSpan day = TimeSpan.Zero;
                TimeSpan night = TimeSpan.Zero;

                for (var time = start; time < end; time = time.AddMinutes(1))
                {
                    bool isHoliday = APSConfig.Holidays.Contains(time.Date);
                    bool isWeekend = time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                    if ((APSConfig.ExcludeHoliday == 1 && isHoliday) || (APSConfig.ExcludeWeekend == 1 && isWeekend))
                        continue;

                    if (IsDayTime(time))
                        day += TimeSpan.FromMinutes(1);
                    else
                        night += TimeSpan.FromMinutes(1);
                }

                result.Add(new ParkingTime
                {
                    Date = currentDate,
                    dayMinutes = (int)day.TotalMinutes,
                    nightMinutes = (int)night.TotalMinutes
                });

                currentDate = currentDate.AddDays(1);
            }

            return result;
        }


        public static bool IsDayTime(DateTime time)
        {
            var range = APSConfig.DayTimeRanges[time.DayOfWeek];
            var t = time.TimeOfDay;

            return t >= range.Start && t < range.End;
        }

        private int Calculate(List<ParkingTime> ptList)
        {
            int totalFee = 0;

            foreach (var pt in ptList)
            {
                int minutes = pt.dayMinutes + pt.nightMinutes;
                int fee = 0;

                bool isWeekend = pt.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                int weekType = isWeekend ? 2 : 1;

                // FeeRules에서 해당 차종 + 주중/주말에 해당하는 스텝만 추출
                var matchedSteps = APSConfig.FeeRules
                    .Where(r => r.Cartype == _CarType && r.Weektype == weekType && r.Parktime.HasValue && r.Parktime.Value > 0)
                    .OrderBy(r => r.Feestep)
                    .Select(r => new ParkFeeStep
                    {
                        UnitMinutes = r.Parktime ?? 0,
                        FeePerUnit = r.Parkfee ?? 0,
                        MaxCount = r.Maxcount ?? 0
                    })
                    .ToList();

                foreach (var step in matchedSteps)
                {
                    int count = step.MaxCount > 0 ? Math.Min(minutes / step.UnitMinutes, step.MaxCount) : minutes / step.UnitMinutes;

                    int usedMinutes = count * step.UnitMinutes;
                    fee += count * step.FeePerUnit;
                    minutes -= usedMinutes;

                    if (minutes <= 0)
                        break;
                }

                // 일일 최대요금 적용
                if (APSConfig.MaxDailyFee > 0)
                    fee = Math.Min(fee, APSConfig.MaxDailyFee);

                pt.Fee = fee;
                totalFee += fee;
            }

            return totalFee;
        }

        private int ReduceMinutes(ParkFeeStep step, ref int remainingMinutes)
        {
            int fee = 0;
            int unit = step.UnitMinutes;
            int maxCount = step.MaxCount;

            while (remainingMinutes >= unit)
            {
                fee += step.FeePerUnit;
                remainingMinutes -= unit;

                if (maxCount > 0)
                {
                    maxCount--;
                    if (maxCount == 0)
                        break;
                }
            }

            return fee;
        }
    }
}
