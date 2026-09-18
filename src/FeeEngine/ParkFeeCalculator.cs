namespace Parking.FeeEngine;

public sealed class ParkingFeeCalculator
{
    private readonly ParkingFeeConfiguration _configuration;
    private readonly HashSet<DateTime> _holidays;

    public ParkingFeeCalculator(ParkingFeeConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _holidays = configuration.Holidays.Select(x => x.Date).ToHashSet();
    }

    public ParkingFeeResult Calculate(ParkingFeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExitAt <= request.EntryAt)
            return new ParkingFeeResult();

        DateTime effectiveEntryAt = request.EntryAt;
        DateTime effectiveExitAt = request.ExitAt;
        int discountMinutes = GetDiscounts(request.DiscountKeys, DiscountType.TimeMinute).Sum(x => x.Value);

        if (_configuration.TimeDiscountApplyType == 1)
            effectiveEntryAt = effectiveEntryAt.AddMinutes(discountMinutes);
        else if (_configuration.TimeDiscountApplyType == 2)
            effectiveExitAt = effectiveExitAt.AddMinutes(-discountMinutes);

        if (effectiveEntryAt >= effectiveExitAt)
            return new ParkingFeeResult();

        List<ParkingDayCharge> dailyCharges = CalculateParkingTime(effectiveEntryAt, effectiveExitAt);
        int originalFee = CalculateDailyFees(dailyCharges, request.CarType);
        int finalFee = ApplyPercentDiscounts(originalFee, request.DiscountKeys);

        return new ParkingFeeResult
        {
            OriginalFee = originalFee,
            FinalFee = finalFee,
            ParkingMinutes = dailyCharges.Sum(x => x.DayMinutes + x.NightMinutes),
            DailyCharges = dailyCharges
        };
    }

    private IReadOnlyList<ParkingDiscount> GetDiscounts(
        IReadOnlyList<int> discountKeys,
        DiscountType discountType)
    {
        var discountsByKey = _configuration.Discounts
            .Where(x => x.Type == discountType)
            .GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.First());

        return discountKeys
            .Where(discountsByKey.ContainsKey)
            .Select(x => discountsByKey[x])
            .ToList();
    }

    private List<ParkingDayCharge> CalculateParkingTime(DateTime entryAt, DateTime exitAt)
    {
        var result = new List<ParkingDayCharge>();

        for (DateTime date = entryAt.Date; date <= exitAt.Date; date = date.AddDays(1))
        {
            DateTime start = date == entryAt.Date ? entryAt : date;
            DateTime end = date == exitAt.Date ? exitAt : date.AddDays(1);
            int dayMinutes = 0;
            int nightMinutes = 0;

            for (DateTime time = start; time < end; time = time.AddMinutes(1))
            {
                bool isWeekend = time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                if ((_configuration.ExcludeHoliday && _holidays.Contains(time.Date)) ||
                    (_configuration.ExcludeWeekend && isWeekend))
                    continue;

                if (IsDayTime(time))
                    dayMinutes++;
                else
                    nightMinutes++;
            }

            result.Add(new ParkingDayCharge
            {
                Date = date,
                DayMinutes = dayMinutes,
                NightMinutes = nightMinutes
            });
        }

        return result;
    }

    private bool IsDayTime(DateTime time)
    {
        return _configuration.DayTimeRanges.TryGetValue(time.DayOfWeek, out DayTimeRange range) &&
               time.TimeOfDay >= range.Start &&
               time.TimeOfDay < range.End;
    }

    private int CalculateDailyFees(List<ParkingDayCharge> dailyCharges, int carType)
    {
        int totalFee = 0;

        foreach (ParkingDayCharge charge in dailyCharges)
        {
            int remainingMinutes = charge.DayMinutes + charge.NightMinutes;
            int weekType = charge.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 2 : 1;
            int fee = 0;

            IEnumerable<ParkingFeeRule> steps = _configuration.FeeRules
                .Where(x => x.CarType == carType && x.WeekType == weekType && x.UnitMinutes > 0)
                .OrderBy(x => x.FeeStep);

            foreach (ParkingFeeRule step in steps)
            {
                int count = remainingMinutes / step.UnitMinutes;
                if (step.MaxCount > 0)
                    count = Math.Min(count, step.MaxCount);

                fee += count * step.FeePerUnit;
                remainingMinutes -= count * step.UnitMinutes;

                if (remainingMinutes <= 0)
                    break;
            }

            if (_configuration.MaxDailyFee > 0)
                fee = Math.Min(fee, _configuration.MaxDailyFee);

            charge.Fee = fee;
            totalFee += fee;
        }

        return totalFee;
    }

    private int ApplyPercentDiscounts(int originalFee, IReadOnlyList<int> discountKeys)
    {
        double finalFee = originalFee;

        foreach (ParkingDiscount discount in GetDiscounts(discountKeys, DiscountType.Percent))
        {
            if (discount.Value > 0)
                finalFee *= 1.0 - discount.Value / 100.0;
        }

        return Math.Max((int)Math.Round(finalFee), 0);
    }
}
