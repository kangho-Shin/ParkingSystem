using Parking.FeeEngine;

namespace Parking.FeeTester;

public sealed partial class MainForm : Form
{
    private readonly ParkingFeeCalculator _calculator = new(CreateSampleConfiguration());

    public MainForm()
    {
        InitializeComponent();
        _entryAt.Value = DateTime.Now.AddHours(-1);
        _exitAt.Value = DateTime.Now;
        _carType.Items.AddRange(new object[] { "1 - 소형", "2 - 중형", "3 - 대형" });
        _carType.SelectedIndex = 0;
    }

    private void CalculateButtonClick(object? sender, EventArgs e) => Calculate();

    private void Calculate()
    {
        if (_exitAt.Value <= _entryAt.Value)
        {
            _result.Text = "출차시각은 입차시각보다 늦어야 합니다.";
            return;
        }

        ParkingFeeResult result = _calculator.Calculate(new ParkingFeeRequest
        {
            EntryAt = _entryAt.Value,
            ExitAt = _exitAt.Value,
            CarType = _carType.SelectedIndex + 1
        });
        _result.Text = $"사이트: {_siteNumber.Value}\r\n그룹: {_groupNumber.Value}\r\n" +
                       $"주차시간: {result.ParkingMinutes:N0}분\r\n" +
                       $"정상요금: {result.OriginalFee:N0}원\r\n" +
                       $"최종요금: {result.FinalFee:N0}원\r\n\r\n" +
                       "현재 단계는 임시 기본 요금설정을 사용합니다.";
    }

    private static ParkingFeeConfiguration CreateSampleConfiguration()
    {
        List<ParkingFeeRule> rules = new();

        for (int weekType = 1; weekType <= 2; weekType++) {
            for (int carType = 1; carType <= 3; carType++)
                rules.Add(new ParkingFeeRule { WeekType = weekType, CarType = carType, FeeStep = 1, UnitMinutes = 10, FeePerUnit = 500 });
        }
        return new ParkingFeeConfiguration
        {
            FeeRules = rules,
            DayTimeRanges = Enum.GetValues<DayOfWeek>()
                                .ToDictionary(day => day, _ => new DayTimeRange(TimeSpan.Zero, TimeSpan.FromHours(24)))
        };
    }

}
