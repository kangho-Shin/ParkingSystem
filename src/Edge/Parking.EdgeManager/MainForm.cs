using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed class MainForm : Form, IEdgeManagerView
{
    private readonly EdgeManagerPresenter _presenter;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 1000 };
    private readonly Label _serviceStatus = CreateStatusLabel("EdgeService");
    private readonly Label _gatewayStatus = CreateStatusLabel("Gateway");
    private readonly Label _centralStatus = CreateStatusLabel("중앙 API");
    private readonly Label _syncStatus = CreateStatusLabel("설정 동기화");
    private readonly Label _outboxStatus = CreateStatusLabel("전송 대기 0건");
    private readonly DataGridView _entryGrid = CreateGrid();
    private readonly DataGridView _activityGrid = CreateGrid();
    private readonly PictureBox _inPicture = CreatePictureBox();
    private readonly PictureBox _outPicture = CreatePictureBox();
    private readonly TreeView _configurationTree = new() { Dock = DockStyle.Fill };

    public MainForm(IEdgeManagementClient client)
    {
        _presenter = new EdgeManagerPresenter(client, this);
        Text = "Parking Edge Manager";
        Width = 1500;
        Height = 900;
        MinimumSize = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BuildLayout();

        _entryGrid.SelectionChanged += EntryGridSelectionChanged;
        _activityGrid.SelectionChanged += ActivityGridSelectionChanged;
        _refreshTimer.Tick += RefreshTimerTick;
        Shown += async (_, _) =>
        {
            await RefreshAsync();
            _refreshTimer.Start();
        };
        FormClosed += (_, _) =>
        {
            _refreshTimer.Stop();
            _presenter.Dispose();
            ReplaceImage(_inPicture, null);
            ReplaceImage(_outPicture, null);
        };
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
    }

    public void ShowDisconnected()
    {
        SetStatus(_serviceStatus, false);
        SetStatus(_gatewayStatus, false);
        SetStatus(_centralStatus, false);
    }

    public void ShowEntries(IReadOnlyList<EdgeEntryItem> entries)
    {
        Guid? selected = (_entryGrid.CurrentRow?.DataBoundItem as EdgeEntryItem)?.EventId;
        _entryGrid.DataSource = entries.ToList();
        RestoreEntrySelection(selected);
    }

    public void ShowActivities(IReadOnlyList<EdgeActivityItem> activities)
    {
        EdgeActivityItem? selected = _activityGrid.CurrentRow?.DataBoundItem as EdgeActivityItem;
        _activityGrid.DataSource = activities.ToList();
        RestoreActivitySelection(selected);
    }

    public void ShowConfiguration(SiteConfiguration? configuration)
    {
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

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        Controls.Add(root);

        FlowLayoutPanel status = new() { Dock = DockStyle.Fill, AutoSize = false };
        status.Controls.AddRange(new Control[]
        {
            _serviceStatus, _gatewayStatus, _centralStatus, _syncStatus, _outboxStatus
        });
        root.Controls.Add(status, 0, 0);

        TableLayoutPanel content = new() { Dock = DockStyle.Fill, ColumnCount = 3 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        content.Controls.Add(CreateGroup("현재 입차 차량", _entryGrid), 0, 0);
        content.Controls.Add(CreateGroup("정산 · 출차 처리", _activityGrid), 1, 0);

        TableLayoutPanel pictures = new() { Dock = DockStyle.Fill, RowCount = 2 };
        pictures.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        pictures.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        pictures.Controls.Add(CreateGroup("입차 사진", _inPicture), 0, 0);
        pictures.Controls.Add(CreateGroup("출차 사진", _outPicture), 0, 1);
        content.Controls.Add(pictures, 2, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(CreateGroup("현장 설정", _configurationTree), 0, 2);
    }

    private async void RefreshTimerTick(object? sender, EventArgs e) => await RefreshAsync();

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
        if (_entryGrid.CurrentRow?.DataBoundItem is EdgeEntryItem entry)
            await _presenter.SelectEntryAsync(entry.EventId, CancellationToken.None);
    }

    private async void ActivityGridSelectionChanged(object? sender, EventArgs e)
    {
        if (_activityGrid.CurrentRow?.DataBoundItem is EdgeActivityItem activity)
            await _presenter.SelectActivityAsync(
                activity.ActivityId, activity.ActivityType, CancellationToken.None);
    }

    private void RestoreEntrySelection(Guid? eventId)
    {
        if (eventId is null) return;
        foreach (DataGridViewRow row in _entryGrid.Rows)
            if (row.DataBoundItem is EdgeEntryItem item && item.EventId == eventId)
                row.Selected = true;
    }

    private void RestoreActivitySelection(EdgeActivityItem? selected)
    {
        if (selected is null) return;
        foreach (DataGridViewRow row in _activityGrid.Rows)
            if (row.DataBoundItem is EdgeActivityItem item &&
                item.ActivityId == selected.ActivityId && item.ActivityType == selected.ActivityType)
                row.Selected = true;
    }

    private static GroupBox CreateGroup(string title, Control control)
    {
        GroupBox group = new() { Text = title, Dock = DockStyle.Fill, Padding = new Padding(6) };
        group.Controls.Add(control);
        return group;
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    private static PictureBox CreatePictureBox() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.Black,
        SizeMode = PictureBoxSizeMode.Zoom
    };

    private static Label CreateStatusLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(8, 10, 12, 0),
        Font = new Font("맑은 고딕", 10, FontStyle.Bold)
    };

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
