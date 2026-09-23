using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class ManualEntryForm : Form
{
    private readonly long _siteId;
    private readonly SiteConfiguration? _configuration;

    public ManualEntryForm()
    {
        InitializeComponent();
    }

    public ManualEntryForm(long siteId, SiteConfiguration configuration) : this()
    {
        _siteId = siteId;
        _configuration = configuration;
        _inDateTime.Value = DateTime.Now;
        _laneCombo.DataSource = configuration.Lanes
            .Where(x => x.Enabled && string.Equals(x.Direction, "ENTRY", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.GroupNumber)
            .Select(x => new SelectionItem<ParkingLane>($"그룹 {x.GroupNumber} / {x.LaneName}", x))
            .ToList();
        UpdateDevices();
    }

    public ManualEntryRequest? Request { get; private set; }

    private void LaneComboSelectedIndexChanged(object? sender, EventArgs e) => UpdateDevices();

    private void UpdateDevices()
    {
        if (_configuration is null || _laneCombo.SelectedItem is not SelectionItem<ParkingLane> lane)
        {
            _deviceCombo.DataSource = null;
            return;
        }

        _deviceCombo.DataSource = VehicleManagementFormPolicy.EntryDevices(_configuration)
            .Where(x => x.LaneId == lane.Value.LaneId)
            .Select(x => new SelectionItem<ParkingDevice>($"{x.DeviceNumber} / {x.DeviceName}", x))
            .ToList();
    }

    private void ConfirmButtonClick(object? sender, EventArgs e)
    {
        string carNumber = _carNumberText.Text.Trim();
        if (_laneCombo.SelectedItem is not SelectionItem<ParkingLane> lane ||
            _deviceCombo.SelectedItem is not SelectionItem<ParkingDevice> device ||
            string.IsNullOrWhiteSpace(carNumber))
        {
            MessageBox.Show("입차 차로, LPR 장치, 차량번호를 모두 입력하세요.", "수동입차",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Request = new ManualEntryRequest(
            _siteId,
            lane.Value.GroupNumber,
            lane.Value.LaneId,
            device.Value.DeviceId,
            carNumber,
            new DateTimeOffset(_inDateTime.Value),
            (int)_carTypeNumber.Value);
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed record SelectionItem<T>(string Text, T Value)
{
    public override string ToString() => Text;
}
