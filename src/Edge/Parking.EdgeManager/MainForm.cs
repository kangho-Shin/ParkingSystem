using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class MainForm : Form, IEdgeManagerView
{
    private readonly EdgeManagerPresenter _presenter;
    private readonly IEdgeManagementClient _client;
    private readonly ICentralParkingClient? _centralClient;
    private readonly long _siteId;
    private SiteConfiguration? _configuration;
    private IReadOnlyList<EdgeEntryItem> _entries = Array.Empty<EdgeEntryItem>();
    private IReadOnlyList<EdgeActivityItem> _activities = Array.Empty<EdgeActivityItem>();
    private IReadOnlyList<ParkingDevice> _devices = Array.Empty<ParkingDevice>();
    private IReadOnlyList<EdgeEntryListItem> _entryRows = Array.Empty<EdgeEntryListItem>();
    private IReadOnlyList<EdgeActivityListItem> _activityRows = Array.Empty<EdgeActivityListItem>();
    private bool _updatingGrids;

    public MainForm()
    {
        InitializeComponent();
        _client = null!;
        _presenter = null!;
    }

    public MainForm(
        IEdgeManagementClient client,
        ICentralParkingClient? centralClient = null,
        long siteId = 0) : this()
    {
        _client = client;
        _centralClient = centralClient;
        _siteId = siteId;
        _presenter = new EdgeManagerPresenter(client, this);
        _entryVehicleButton.Enabled = false;
        _exitVehicleButton.Enabled = false;
        ConfigureGridColumns();
    }

    public void ShowStatus(EdgeServiceStatus status)
    {
        SetStatus(_serviceStatus, status.ServiceConnected);
        SetStatus(_gatewayStatus, status.GatewayConnected);
        SetStatus(_centralStatus, status.CentralConnected);
        _syncStatus.Text = status.ConfigurationSyncedAt is null
            ? "설정 동기화: 없음"
            : $"설정 동기화: {status.ConfigurationSyncedAt:yyyy-MM-dd HH:mm:ss}";
        _outboxStatus.Text = $"전송 대기: {status.PendingOutboxCount:N0}건";
        _lprStatus.Text = $"LPR 접속: {status.LprConnectionCount:N0}대";
    }

    public void ShowDisconnected()
    {
        SetStatus(_serviceStatus, false);
        SetStatus(_gatewayStatus, false);
        SetStatus(_centralStatus, false);
    }

    public void ShowEntries(IReadOnlyList<EdgeEntryItem> entries)
    {
        _entries = entries;
        BindEntryRows(EdgeManagerListMapper.MapEntries(entries, _devices));
    }

    public void ShowActivities(IReadOnlyList<EdgeActivityItem> activities)
    {
        _activities = activities;
        BindActivityRows(EdgeManagerListMapper.MapActivities(activities, _devices));
    }

    public void ShowConfiguration(SiteConfiguration? configuration)
    {
        _configuration = configuration;
        _devices = configuration?.Devices ?? Array.Empty<ParkingDevice>();
        bool managementEnabled = _centralClient is not null &&
            _siteId > 0 && configuration is not null;
        _entryVehicleButton.Enabled = managementEnabled;
        _exitVehicleButton.Enabled = managementEnabled;
        BindEntryRows(EdgeManagerListMapper.MapEntries(_entries, _devices));
        BindActivityRows(EdgeManagerListMapper.MapActivities(_activities, _devices));
        _configurationTree.BeginUpdate();
        _configurationTree.Nodes.Clear();
        if (configuration is not null)
        {
            TreeNode site = _configurationTree.Nodes.Add(
                $"현장 {configuration.Site.SiteId}: {configuration.Site.SiteName}");
            TreeNode lanes = site.Nodes.Add("차로");
            foreach (ParkingLane lane in configuration.Lanes)
                lanes.Nodes.Add($"{lane.LaneId} / 그룹 {lane.GroupNumber} / {lane.LaneName} / {lane.Direction}");
            TreeNode devices = site.Nodes.Add("장비");
            foreach (ParkingDevice device in configuration.Devices)
                devices.Nodes.Add($"{device.DeviceId} / {device.DeviceType} / {device.DeviceName} / {device.IpAddress}:{device.Port}");
            site.Expand();
            lanes.Expand();
            devices.Expand();
        }
        _configurationTree.EndUpdate();
    }

    public void ShowImages(byte[]? inImage, byte[]? outImage)
    {
        ReplaceImage(_inPicture, CreateImage(inImage));
        ReplaceImage(_outPicture, CreateImage(outImage));
    }

    private async void MainFormShown(object? sender, EventArgs e)
    {
        await RefreshAsync();
        _refreshTimer.Start();
    }

    private void MainFormFormClosed(object? sender, FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        _presenter?.Dispose();
        ReplaceImage(_inPicture, null);
        ReplaceImage(_outPicture, null);
    }

    private async void RefreshTimerTick(object? sender, EventArgs e) => await RefreshAsync();

    private async void ConfigurationButtonClick(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _configurationButton.Enabled = false;
        try
        {
            EdgeSetupResponse? setup = await _client.GetSetupAsync(CancellationToken.None);
            if (setup is null) return;
            SiteConfiguration? configuration = await _client.GetConfigurationAsync(CancellationToken.None);
            using ConfigurationForm form = new(_client, setup, configuration);
            form.ShowDialog(this);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"환경설정을 열지 못했습니다.\r\n{exception.Message}", "현장 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _configurationButton.Enabled = true;
            _refreshTimer.Start();
        }
    }

    private async void EntryVehicleButtonClick(object? sender, EventArgs e)
    {
        if (_centralClient is null || _configuration is null) return;
        _refreshTimer.Stop();
        _entryVehicleButton.Enabled = false;
        try
        {
            using EntryVehicleForm form = new(
                _centralClient, _client, _siteId, _configuration);
            form.ShowDialog(this);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"입차차량 관리 화면을 열지 못했습니다.\r\n{exception.Message}",
                "입차차량 관리", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _entryVehicleButton.Enabled = _centralClient is not null && _configuration is not null;
            _refreshTimer.Start();
        }
    }

    private async void ExitVehicleButtonClick(object? sender, EventArgs e)
    {
        if (_centralClient is null || _configuration is null) return;
        _refreshTimer.Stop();
        _exitVehicleButton.Enabled = false;
        try
        {
            using ExitVehicleForm form = new(
                _centralClient, _client, _siteId, _configuration);
            form.ShowDialog(this);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"출차차량 조회 화면을 열지 못했습니다.\r\n{exception.Message}",
                "출차차량 조회", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _exitVehicleButton.Enabled = _centralClient is not null && _configuration is not null;
            _refreshTimer.Start();
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _presenter.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            ShowDisconnected();
        }
    }

    private async void EntryGridSelectionChanged(object? sender, EventArgs e)
    {
        if (!_updatingGrids && _entryGrid.CurrentRow?.DataBoundItem is EdgeEntryListItem entry)
            await _presenter.SelectEntryAsync(entry.EventId, CancellationToken.None);
    }

    private async void ActivityGridSelectionChanged(object? sender, EventArgs e)
    {
        if (!_updatingGrids &&
            _activityGrid.CurrentRow?.DataBoundItem is EdgeActivityListItem activity)
            await _presenter.SelectActivityAsync(
                activity.ActivityId, activity.ActivityType, CancellationToken.None);
    }

    private void BindEntryRows(IReadOnlyList<EdgeEntryListItem> rows)
    {
        if (_entryRows.SequenceEqual(rows)) return;
        Guid? selected = (_entryGrid.CurrentRow?.DataBoundItem as EdgeEntryListItem)?.EventId;
        GridScrollState scroll = CaptureScroll(_entryGrid);
        _updatingGrids = true;
        try
        {
            _entryRows = rows;
            _entryGrid.DataSource = rows.ToList();
            RestoreEntrySelection(selected);
            RestoreScroll(_entryGrid, scroll);
        }
        finally { _updatingGrids = false; }
    }

    private void BindActivityRows(IReadOnlyList<EdgeActivityListItem> rows)
    {
        if (_activityRows.SequenceEqual(rows)) return;
        EdgeActivityListItem? selected =
            _activityGrid.CurrentRow?.DataBoundItem as EdgeActivityListItem;
        GridScrollState scroll = CaptureScroll(_activityGrid);
        _updatingGrids = true;
        try
        {
            _activityRows = rows;
            _activityGrid.DataSource = rows.ToList();
            RestoreActivitySelection(selected);
            RestoreScroll(_activityGrid, scroll);
        }
        finally { _updatingGrids = false; }
    }

    private void RestoreEntrySelection(Guid? eventId)
    {
        if (eventId is null) return;
        foreach (DataGridViewRow row in _entryGrid.Rows)
        {
            if (row.DataBoundItem is not EdgeEntryListItem item || item.EventId != eventId)
                continue;
            row.Selected = true;
            _entryGrid.CurrentCell = row.Cells[0];
            return;
        }
    }

    private void RestoreActivitySelection(EdgeActivityListItem? selected)
    {
        if (selected is null) return;
        foreach (DataGridViewRow row in _activityGrid.Rows)
        {
            if (row.DataBoundItem is not EdgeActivityListItem item ||
                item.ActivityId != selected.ActivityId ||
                item.ActivityType != selected.ActivityType)
                continue;
            row.Selected = true;
            _activityGrid.CurrentCell = row.Cells[0];
            return;
        }
    }

    private void ConfigureGridColumns()
    {
        if (_entryGrid.Columns.Count == 0)
        {
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.DeviceNumber), "장치번호");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.DeviceName), "장치이름");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.CarNumber), "차량번호");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.InDateTime), "입차시간", "yyyy-MM-dd HH:mm:ss");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.ParkingSessionId), "주차번호");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.Groupnum), "그룹");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.LaneId), "차로");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.DeliveryState), "전송상태");
            AddTextColumn(_entryGrid, nameof(EdgeEntryListItem.ResultCode), "처리결과");
        }
        if (_activityGrid.Columns.Count == 0)
        {
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.DeviceNumber), "장치번호");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.DeviceName), "장치이름");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.CarNumber), "차량번호");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.OccurredAt), "처리시간", "yyyy-MM-dd HH:mm:ss");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.ActivityName), "처리구분");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.ParkingSessionId), "주차번호");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.Groupnum), "그룹");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.LaneId), "차로");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.PaidAmount), "결제금액", "N0");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.OpenBarrier), "차단기");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.DeliveryState), "전송상태");
            AddTextColumn(_activityGrid, nameof(EdgeActivityListItem.ResultCode), "처리결과");
        }
    }

    private static void AddTextColumn(DataGridView grid, string propertyName, string headerText, string? format = null)
    {
        DataGridViewTextBoxColumn column = new()
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Name = propertyName
        };
        if (format is not null) column.DefaultCellStyle.Format = format;
        grid.Columns.Add(column);
    }

    private static GridScrollState CaptureScroll(DataGridView grid) => new(
        grid.FirstDisplayedScrollingRowIndex,
        grid.HorizontalScrollingOffset);

    private static void RestoreScroll(DataGridView grid, GridScrollState state)
    {
        if (state.FirstRowIndex >= 0 && grid.Rows.Count > 0)
            grid.FirstDisplayedScrollingRowIndex = Math.Min(
                state.FirstRowIndex, grid.Rows.Count - 1);
        grid.HorizontalScrollingOffset = state.HorizontalOffset;
    }

    private sealed record GridScrollState(int FirstRowIndex, int HorizontalOffset);

    private static void SetStatus(Label label, bool connected)
    {
        string name = label.Text.Split(':')[0];
        label.Text = $"{name}: {(connected ? "정상" : "끊김")}";
        label.ForeColor = connected ? Color.DarkGreen : Color.Red;
    }

    private static Image? CreateImage(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return null;
        try
        {
            using MemoryStream stream = new(bytes);
            using Image source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void ReplaceImage(PictureBox pictureBox, Image? image)
    {
        Image? old = pictureBox.Image;
        pictureBox.Image = image;
        old?.Dispose();
    }
}
