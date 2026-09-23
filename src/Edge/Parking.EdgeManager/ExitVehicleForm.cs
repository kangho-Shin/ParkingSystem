using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class ExitVehicleForm : Form
{
    private readonly ICentralParkingClient _centralClient;
    private readonly IEdgeManagementClient _edgeClient;
    private readonly long _siteId;
    private readonly SiteConfiguration _configuration;
    private int _page = 1;
    private int _pageSize = 200;
    private long _totalCount;
    private CancellationTokenSource? _imageCancellation;

    public ExitVehicleForm()
    {
        InitializeComponent();
        _centralClient = null!;
        _edgeClient = null!;
        _configuration = null!;
    }

    public ExitVehicleForm(
        ICentralParkingClient centralClient,
        IEdgeManagementClient edgeClient,
        long siteId,
        SiteConfiguration configuration) : this()
    {
        _centralClient = centralClient;
        _edgeClient = edgeClient;
        _siteId = siteId;
        _configuration = configuration;
        ConfigureFilters();
        ConfigureGrid();
    }

    private async void ExitVehicleFormShown(object? sender, EventArgs e) => await SearchAsync();

    private void ConfigureFilters()
    {
        (DateTimeOffset from, DateTimeOffset to) =
            VehicleManagementFormPolicy.DefaultExitRange(DateTimeOffset.Now);
        _fromDate.Value = from.LocalDateTime;
        _toDate.Value = to.LocalDateTime;

        _statusCombo.Items.Add(new SelectionItem<string?>("전체 상태", null));
        _statusCombo.Items.Add(new SelectionItem<string?>("정산완료", "X"));
        _statusCombo.Items.Add(new SelectionItem<string?>("출차완료", "O"));
        _statusCombo.SelectedIndex = 0;

        _groupCombo.Items.Add(new SelectionItem<int?>("전체 그룹", null));
        foreach (int group in _configuration.Lanes.Select(x => x.GroupNumber).Distinct().Order())
            _groupCombo.Items.Add(new SelectionItem<int?>($"그룹 {group}", group));
        _groupCombo.SelectedIndex = 0;

        _deviceCombo.Items.Add(new SelectionItem<long?>("전체 장치", null));
        foreach (ParkingDevice device in _configuration.Devices.Where(x => x.Enabled).OrderBy(x => x.DeviceNumber))
            _deviceCombo.Items.Add(new SelectionItem<long?>($"{device.DeviceNumber} / {device.DeviceName}", device.DeviceId));
        _deviceCombo.SelectedIndex = 0;
    }

    private void ConfigureGrid()
    {
        AddColumn(nameof(ParkingManagementItem.SessionType), "구분");
        AddColumn(nameof(ParkingManagementItem.ParkingSessionId), "주차번호");
        AddColumn(nameof(ParkingManagementItem.CarNumber), "차량번호");
        AddColumn(nameof(ParkingManagementItem.Status), "상태");
        AddColumn(nameof(ParkingManagementItem.Groupnum), "그룹");
        AddColumn(nameof(ParkingManagementItem.InDeviceName), "입차장치");
        AddColumn(nameof(ParkingManagementItem.InDateTime), "입차시간", "yyyy-MM-dd HH:mm:ss");
        AddColumn(nameof(ParkingManagementItem.ProcessDeviceName), "처리장치");
        AddColumn(nameof(ParkingManagementItem.PaidAt), "정산시간", "yyyy-MM-dd HH:mm:ss");
        AddColumn(nameof(ParkingManagementItem.OutDateTime), "출차시간", "yyyy-MM-dd HH:mm:ss");
        AddColumn(nameof(ParkingManagementItem.ParkingMinutes), "주차시간(분)", "N0");
        AddColumn(nameof(ParkingManagementItem.OriginalFee), "주차요금", "N0");
        AddColumn(nameof(ParkingManagementItem.DiscountFee), "할인금액", "N0");
        AddColumn(nameof(ParkingManagementItem.PaidFee), "결제금액", "N0");
    }

    private void AddColumn(string propertyName, string headerText, string? format = null)
    {
        DataGridViewTextBoxColumn column = new()
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Name = propertyName
        };
        if (format is not null) column.DefaultCellStyle.Format = format;
        _grid.Columns.Add(column);
    }

    private ParkingManagementQuery Query()
    {
        DateTimeOffset from = new(_fromDate.Value);
        DateTimeOffset to = new(_toDate.Value);
        ParkingManagementQueryPolicy.ValidateExitRange(from, to);
        return new ParkingManagementQuery(
            _siteId,
            from,
            to,
            (_groupCombo.SelectedItem as SelectionItem<int?>)?.Value,
            (_deviceCombo.SelectedItem as SelectionItem<long?>)?.Value,
            string.IsNullOrWhiteSpace(_carNumberText.Text) ? null : _carNumberText.Text.Trim(),
            (_statusCombo.SelectedItem as SelectionItem<string?>)?.Value,
            _page,
            _pageSize);
    }

    private async Task SearchAsync()
    {
        SetBusy(true);
        try
        {
            PagedParkingResult<ParkingManagementItem> result =
                await _centralClient.SearchExitsAsync(Query(), CancellationToken.None);
            _totalCount = result.TotalCount;
            _page = result.Page;
            _pageSize = result.PageSize;
            _grid.DataSource = result.Items.ToList();
            UpdatePageStatus();
            await LoadSelectedImagesAsync();
        }
        catch (TaskCanceledException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
            MessageBox.Show("조회 기간은 시작일 이후이며 최대 31일까지 지정할 수 있습니다.", "출차차량 조회",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"출차차량을 조회하지 못했습니다.\r\n{exception.Message}", "출차차량 조회",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        _searchButton.Enabled = !busy;
        _closeButton.Enabled = true;
        if (busy)
        {
            _previousButton.Enabled = false;
            _nextButton.Enabled = false;
        }
        else UpdatePageStatus();
    }

    private void UpdatePageStatus()
    {
        long lastPage = Math.Max(1, (_totalCount + _pageSize - 1) / _pageSize);
        _pageLabel.Text = $"{_page:N0} / {lastPage:N0} 페이지 · 총 {_totalCount:N0}건";
        _previousButton.Enabled = _page > 1;
        _nextButton.Enabled = _page < lastPage;
    }

    private async void SearchButtonClick(object? sender, EventArgs e)
    {
        _page = 1;
        await SearchAsync();
    }

    private async void PreviousButtonClick(object? sender, EventArgs e)
    {
        if (_page <= 1) return;
        _page--;
        await SearchAsync();
    }

    private async void NextButtonClick(object? sender, EventArgs e)
    {
        _page++;
        await SearchAsync();
    }

    private async void GridSelectionChanged(object? sender, EventArgs e) =>
        await LoadSelectedImagesAsync();

    private async Task LoadSelectedImagesAsync()
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        _imageCancellation = new CancellationTokenSource();
        CancellationToken token = _imageCancellation.Token;
        try
        {
            if (_grid.CurrentRow?.DataBoundItem is not ParkingManagementItem item)
            {
                ReplaceImage(_inPicture, null);
                ReplaceImage(_outPicture, null);
                return;
            }
            Task<byte[]?> inTask = _edgeClient.GetImageAsync(item.InImage, token);
            Task<byte[]?> outTask = _edgeClient.GetImageAsync(item.OutImage, token);
            await Task.WhenAll(inTask, outTask);
            if (!token.IsCancellationRequested)
            {
                ReplaceImage(_inPicture, await inTask);
                ReplaceImage(_outPicture, await outTask);
                _imageStatus.Text = "";
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested)
                _imageStatus.Text = $"사진 조회 실패: {exception.Message}";
        }
    }

    private static void ReplaceImage(PictureBox picture, byte[]? bytes)
    {
        Image? image = null;
        if (bytes is { Length: > 0 })
        {
            try
            {
                using MemoryStream stream = new(bytes);
                using Image source = Image.FromStream(stream);
                image = new Bitmap(source);
            }
            catch (ArgumentException) { }
        }
        Image? old = picture.Image;
        picture.Image = image;
        old?.Dispose();
    }

    private void ExitVehicleFormFormClosed(object? sender, FormClosedEventArgs e)
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        ReplaceImage(_inPicture, null);
        ReplaceImage(_outPicture, null);
    }
}
