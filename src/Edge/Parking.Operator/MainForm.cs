using Microsoft.Extensions.Logging;
using Parking.Contracts;

namespace Parking.Operator;

public sealed partial class MainForm : Form
{
    private readonly OperatorClient _client;
    private readonly ILogger<MainForm> _logger;
    private SiteConfiguration? _configuration;
    private long _siteId;
    private ParkingSearchCandidate? _selected;
    private OperatorFeeQuote? _quote;

    public MainForm()
    {
        InitializeComponent();
        _client = null!;
        _logger = null!;
    }

    public MainForm(OperatorClient client, ILogger<MainForm> logger) : this()
    {
        _client = client;
        _logger = logger;
    }

    private async void MainFormShown(object? sender, EventArgs e) => await InitializeAsync();
    private async void CandidatesSelectionChanged(object? sender, EventArgs e) => await CandidateSelectedAsync();
    private async void SearchButtonClick(object? sender, EventArgs e) => await SearchAsync();
    private async void QuoteButtonClick(object? sender, EventArgs e) => await QuoteAsync();
    private async void CardButtonClick(object? sender, EventArgs e) => await PayAsync("Card");
    private async void CashButtonClick(object? sender, EventArgs e) => await PayAsync("Cash");
    private async void CorrectButtonClick(object? sender, EventArgs e) => await CorrectAsync();
    private async void ManualEntryButtonClick(object? sender, EventArgs e) => await ManualEntryAsync();
    private async void ManualExitButtonClick(object? sender, EventArgs e) => await ManualExitAsync();
    private async void OpenBarrierButtonClick(object? sender, EventArgs e) => await OpenBarrierAsync();
    private void MainFormFormClosed(object? sender, FormClosedEventArgs e)
    {
        Image? image = _inImage.Image;
        _inImage.Image = null;
        image?.Dispose();
    }

    private async Task InitializeAsync() => await RunAsync(async () =>
    {
        EdgeServiceStatus status = await _client.GetStatusAsync(CancellationToken.None);
        _configuration = await _client.GetConfigurationAsync(CancellationToken.None);
        _siteId = _configuration.Site.SiteId;
        BindDevices(_entryDevice, "Entry");
        BindDevices(_exitDevice, "Exit");
        _status.Text = $"EdgeService 정상 / Gateway {(status.GatewayConnected ? "정상" : "끊김")} / 중앙 {(status.CentralConnected ? "정상" : "끊김")}";
    });

    private void BindDevices(ComboBox combo, string direction)
    {
        if (_configuration is null) return;
        List<DeviceChoice> devices = (from device in _configuration.Devices
            join lane in _configuration.Lanes on device.LaneId equals (long?)lane.LaneId
            where device.Enabled && lane.Enabled && lane.Direction.Equals(direction, StringComparison.OrdinalIgnoreCase)
            select new DeviceChoice(device.DeviceId, lane.LaneId, lane.GroupNumber, $"{device.DeviceName} / {lane.LaneName}")).ToList();
        combo.DataSource = devices;
        combo.DisplayMember = nameof(DeviceChoice.Name);
    }

    private async Task SearchAsync() => await RunAsync(async () =>
    {
        OperatorSearchResult result = await _client.SearchAsync(_siteId, (int)_groupnum.Value, RequireCarNumber(), DateTimeOffset.Now, CancellationToken.None);
        if (result.Quote is not null)
        {
            _quote = result.Quote;
            _selected = new(result.Quote.ParkingSessionId, result.Quote.CarNumber, (int)_groupnum.Value, 1, result.Quote.EntryAt, null);
            _candidates.DataSource = new[] { _selected };
            ShowFee(result.Quote);
        }
        else
        {
            _quote = null;
            _selected = null;
            _candidates.DataSource = result.Candidates.ToList();
            _fee.Text = result.Candidates.Count == 0 ? "조회 결과 없음" : $"{result.Candidates.Count}대 중 차량을 선택하세요.";
        }
    });

    private async Task CandidateSelectedAsync()
    {
        if (_candidates.CurrentRow?.DataBoundItem is not ParkingSearchCandidate candidate) return;
        _selected = candidate;
        _carNumber.Text = candidate.CarNumber;
        _groupnum.Value = candidate.Groupnum;
        await LoadImageAsync(candidate.InImage);
        if (_quote?.ParkingSessionId != candidate.ParkingSessionId) await QuoteAsync();
    }

    private async Task QuoteAsync() => await RunAsync(async () =>
    {
        if (_selected is null) throw new InvalidOperationException("차량을 먼저 선택하세요.");
        _quote = await _client.QuoteAsync(_selected.ParkingSessionId, DateTimeOffset.Now, ParseDiscountKeys(), CancellationToken.None);
        ShowFee(_quote);
    });

    private void ShowFee(OperatorFeeQuote quote) => _fee.Text = $"{quote.Fee.ParkingMinutes:N0}분 / 정상 {quote.Fee.OriginalFee:N0}원 / 할인 {quote.Fee.DiscountFee:N0}원 / 결제 {quote.PayableAmount:N0}원";

    private async Task PayAsync(string method) => await RunAsync(async () =>
    {
        if (_quote is null) throw new InvalidOperationException("요금을 먼저 계산하세요.");
        PaymentCompleteResponse result = await _client.PayAsync(new CompletePaymentRequest
        {
            PaymentId = Guid.NewGuid(), ParkingSessionId = _quote.ParkingSessionId, SiteId = _siteId,
            OriginalFee = _quote.Fee.OriginalFee, DiscountFee = _quote.Fee.DiscountFee, PaidAmount = _quote.PayableAmount,
            PaymentMethod = method, ApprovalNumber = method == "Cash" ? "CASH" : $"TEST-{DateTime.Now:HHmmss}", PaidAt = DateTimeOffset.Now
        }, CancellationToken.None);
        MessageBox.Show(result.Message, "결제 결과");
        await QuoteAsync();
    });

    private async Task CorrectAsync() => await RunAsync(async () =>
    {
        if (_selected is null) throw new InvalidOperationException("차량을 먼저 선택하세요.");
        CorrectCarNumberResponse result = await _client.CorrectCarNumberAsync(_selected.ParkingSessionId, RequireCarNumber(), CancellationToken.None);
        MessageBox.Show(result.Message, "차량번호 수정");
        await SearchAsync();
    });

    private async Task ManualEntryAsync() => await RunAsync(async () =>
    {
        DeviceChoice device = RequireDevice(_entryDevice);
        FieldEventResponse result = await _client.ManualEntryAsync(new FieldEventRequest(Guid.NewGuid(), _siteId, device.LaneId, device.DeviceId, RequireCarNumber(), DateTimeOffset.Now, device.Groupnum), CancellationToken.None);
        MessageBox.Show(result.DisplayMessage, "수동 입차");
    });

    private async Task ManualExitAsync() => await RunAsync(async () =>
    {
        DeviceChoice device = RequireDevice(_exitDevice);
        FieldEventResponse result = await _client.ManualExitAsync(new ExitEventRequest(Guid.NewGuid(), _siteId, device.LaneId, device.DeviceId, RequireCarNumber(), DateTimeOffset.Now, device.Groupnum, _selected?.CarType ?? 1, ParseDiscountKeys().ToList()), CancellationToken.None);
        MessageBox.Show(result.DisplayMessage, "수동 출차");
    });

    private async Task OpenBarrierAsync() => await RunAsync(async () =>
    {
        DeviceChoice device = RequireDevice(_exitDevice);
        await _client.OpenBarrierAsync(new OperatorBarrierRequest(_siteId, device.LaneId, device.DeviceId, _carNumber.Text.Trim()), CancellationToken.None);
        MessageBox.Show("차단기 개방 명령을 전송했습니다.", "차단기");
    });

    private async Task LoadImageAsync(string? fileName)
    {
        byte[]? bytes = await _client.GetImageAsync(fileName, CancellationToken.None);
        Image? image = bytes is null ? null : Image.FromStream(new MemoryStream(bytes));
        Image? old = _inImage.Image;
        _inImage.Image = image;
        old?.Dispose();
    }

    private string RequireCarNumber() => string.IsNullOrWhiteSpace(_carNumber.Text) ? throw new InvalidOperationException("차량번호를 입력하세요.") : _carNumber.Text.Trim();
    private static DeviceChoice RequireDevice(ComboBox combo) => combo.SelectedItem as DeviceChoice ?? throw new InvalidOperationException("차로 장비를 선택하세요.");
    private IReadOnlyList<int> ParseDiscountKeys() => _discountKeys.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => int.TryParse(x, out int key) ? key : 0).Where(x => x > 0).Distinct().ToList();

    private async Task RunAsync(Func<Task> action)
    {
        try { UseWaitCursor = true; await action(); }
        catch (Exception exception) { _logger.LogError(exception, "운영 작업 실패"); MessageBox.Show(exception.Message, "Parking Operator", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { UseWaitCursor = false; }
    }

    private sealed record DeviceChoice(long DeviceId, long LaneId, int Groupnum, string Name);
}
