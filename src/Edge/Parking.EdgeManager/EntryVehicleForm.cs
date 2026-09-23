using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class EntryVehicleForm : Form
{
    private readonly ICentralParkingClient _centralClient;
    private readonly IEdgeManagementClient _edgeClient;
    private readonly long _siteId;
    private readonly SiteConfiguration _configuration;
    private int _page = 1;
    private int _pageSize = 200;
    private long _totalCount;
    private CancellationTokenSource? _imageCancellation;

    public EntryVehicleForm()
    {
        InitializeComponent();
        _centralClient = null!;
        _edgeClient = null!;
        _configuration = null!;
    }

    public EntryVehicleForm(
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

    private async void EntryVehicleFormShown(object? sender, EventArgs e) => await SearchAsync();

    private void ConfigureFilters()
    {
        _fromDate.Value = DateTime.Today;
        _toDate.Value = DateTime.Now;
        _groupCombo.Items.Add(new SelectionItem<int?>("전체 그룹", null));
        foreach (int group in _configuration.Lanes.Select(x => x.GroupNumber).Distinct().Order())
            _groupCombo.Items.Add(new SelectionItem<int?>($"그룹 {group}", group));
        _groupCombo.SelectedIndex = 0;

        _deviceCombo.Items.Add(new SelectionItem<long?>("전체 장치", null));
        foreach (ParkingDevice device in VehicleManagementFormPolicy.EntryDevices(_configuration))
            _deviceCombo.Items.Add(new SelectionItem<long?>($"{device.DeviceNumber} / {device.DeviceName}", device.DeviceId));
        _deviceCombo.SelectedIndex = 0;
    }

    private void ConfigureGrid()
    {
        AddColumn(nameof(ParkingManagementItem.SessionType), "구분");
        AddColumn(nameof(ParkingManagementItem.ParkingSessionId), "주차번호");
        AddColumn(nameof(ParkingManagementItem.CarNumber), "차량번호");
        AddColumn(nameof(ParkingManagementItem.CarType), "차종");
        AddColumn(nameof(ParkingManagementItem.Groupnum), "그룹");
        AddColumn(nameof(ParkingManagementItem.InDeviceName), "입차장치");
        AddColumn(nameof(ParkingManagementItem.InDateTime), "입차시간", "yyyy-MM-dd HH:mm:ss");
        AddColumn(nameof(ParkingManagementItem.ParkingMinutes), "주차시간(분)", "N0");
        AddColumn(nameof(ParkingManagementItem.IsManual), "수동입차");
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

    private ParkingManagementQuery Query() => new(
        _siteId,
        _useFrom.Checked ? new DateTimeOffset(_fromDate.Value) : null,
        _useTo.Checked ? new DateTimeOffset(_toDate.Value) : null,
        (_groupCombo.SelectedItem as SelectionItem<int?>)?.Value,
        (_deviceCombo.SelectedItem as SelectionItem<long?>)?.Value,
        string.IsNullOrWhiteSpace(_carNumberText.Text) ? null : _carNumberText.Text.Trim(),
        Page: _page,
        PageSize: _pageSize);

    private async Task SearchAsync()
    {
        ApplyState(VehicleManagementFormPolicy.RequestStarted());
        try
        {
            PagedParkingResult<ParkingManagementItem> result =
                await _centralClient.SearchEntriesAsync(Query(), CancellationToken.None);
            _totalCount = result.TotalCount;
            _page = result.Page;
            _pageSize = result.PageSize;
            _grid.DataSource = result.Items.ToList();
            UpdatePageStatus();
            await LoadSelectedImageAsync();
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            MessageBox.Show($"입차차량을 조회하지 못했습니다.\r\n{exception.Message}", "입차차량 관리",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ApplyState(VehicleManagementFormPolicy.RequestCompleted());
        }
    }

    private void ApplyState(VehicleManagementButtonState state)
    {
        _searchButton.Enabled = state.SearchEnabled;
        _closeButton.Enabled = state.CloseEnabled;
        _manualEntryButton.Enabled = state.EditEnabled;
        _changeCarNumberButton.Enabled = state.EditEnabled && _grid.CurrentRow is not null;
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

    private async void ManualEntryButtonClick(object? sender, EventArgs e)
    {
        using ManualEntryForm form = new(_siteId, _configuration);
        if (form.ShowDialog(this) != DialogResult.OK || form.Request is null) return;
        ApplyState(VehicleManagementFormPolicy.RequestStarted());
        try
        {
            ManualEntryResponse response = await _centralClient.CreateManualEntryAsync(
                form.Request, CancellationToken.None);
            MessageBox.Show(response.Message, "수동입차",
                MessageBoxButtons.OK,
                response.Accepted ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (response.Accepted)
            {
                _page = 1;
                await SearchAsync();
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            MessageBox.Show($"수동입차를 처리하지 못했습니다.\r\n{exception.Message}", "수동입차",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { ApplyState(VehicleManagementFormPolicy.RequestCompleted()); }
    }

    private async void ChangeCarNumberButtonClick(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not ParkingManagementItem item) return;
        using CarNumberChangeForm form = new(item.CarNumber);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        ApplyState(VehicleManagementFormPolicy.RequestStarted());
        try
        {
            await _centralClient.CorrectCarNumberAsync(
                item.SessionType,
                item.ParkingSessionId,
                new ManagementCarNumberRequest(_siteId, form.CarNumber),
                CancellationToken.None);
            await SearchAsync();
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            MessageBox.Show($"차량번호를 변경하지 못했습니다.\r\n{exception.Message}", "차량번호 변경",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { ApplyState(VehicleManagementFormPolicy.RequestCompleted()); }
    }

    private async void GridSelectionChanged(object? sender, EventArgs e)
    {
        _changeCarNumberButton.Enabled = _grid.CurrentRow is not null && _searchButton.Enabled;
        await LoadSelectedImageAsync();
    }

    private async Task LoadSelectedImageAsync()
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        _imageCancellation = new CancellationTokenSource();
        CancellationToken token = _imageCancellation.Token;
        try
        {
            byte[]? bytes = _grid.CurrentRow?.DataBoundItem is ParkingManagementItem item
                ? await _edgeClient.GetImageAsync(item.InImage, token)
                : null;
            if (!token.IsCancellationRequested) ReplaceImage(bytes);
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

    private void ReplaceImage(byte[]? bytes)
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
        Image? old = _picture.Image;
        _picture.Image = image;
        old?.Dispose();
        _imageStatus.Text = image is null ? "입차 사진 없음" : "";
    }

    private void EntryVehicleFormFormClosed(object? sender, FormClosedEventArgs e)
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        ReplaceImage(null);
    }
}
