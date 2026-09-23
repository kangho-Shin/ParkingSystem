using System.Data;
using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class ConfigurationForm : Form
{
    private readonly IEdgeManagementClient _client;
    private readonly long _siteId;

    public ConfigurationForm()
    {
        InitializeComponent();
        _client = null!;
    }

    public ConfigurationForm(
        IEdgeManagementClient client,
        EdgeSetupResponse setup,
        SiteConfiguration? configuration) : this()
    {
        _client = client;
        _siteId = setup.SiteId;
        Text = $"현장 설정 - {setup.SiteId}";

        AddSitePage(configuration?.Site ?? new ParkingSite(_siteId, "", true));
        AddLanePage(configuration?.Lanes ?? []);
        AddDevicePage(configuration?.Devices ?? []);
        AddLinkPage(configuration?.DeviceLinks ?? []);
        AddVariablePage(configuration?.OperationVariables ?? []);
    }

    private void AddSitePage(ParkingSite site)
    {
        DataTable table = CreateTable(
            ("SiteId", typeof(long)), ("SiteName", typeof(string)), ("Enabled", typeof(bool)));
        table.Rows.Add(site.SiteId, site.SiteName, site.Enabled);
        AddPage("현장", table, false,
            async row => await _client.SaveSiteAsync(new ParkingSite(
                _siteId, TextValue(row, "SiteName"), BoolValue(row, "Enabled")), CancellationToken.None),
            null);
    }

    private void AddLanePage(IReadOnlyList<ParkingLane> values)
    {
        DataTable table = CreateTable(
            ("LaneId", typeof(long)), ("Groupnum", typeof(int)), ("LaneName", typeof(string)),
            ("Direction", typeof(string)), ("Enabled", typeof(bool)));
        foreach (ParkingLane x in values)
            table.Rows.Add(x.LaneId, x.GroupNumber, x.LaneName, x.Direction, x.Enabled);
        AddPage("차로", table, true,
            async row => await _client.SaveLaneAsync(new ParkingLane(
                LongValue(row, "LaneId"), _siteId, IntValue(row, "Groupnum"),
                TextValue(row, "LaneName"), TextValue(row, "Direction"), BoolValue(row, "Enabled")), CancellationToken.None),
            async row => await _client.DeleteLaneAsync(LongValue(row, "LaneId"), CancellationToken.None));
    }

    private void AddDevicePage(IReadOnlyList<ParkingDevice> values)
    {
        DataTable table = CreateTable(
            ("DeviceId", typeof(long)), ("LaneId", typeof(long)), ("DeviceNumber", typeof(int)),
            ("DeviceType", typeof(string)), ("DeviceName", typeof(string)), ("IpAddress", typeof(string)),
            ("Port", typeof(int)), ("Enabled", typeof(bool)));
        table.Columns["LaneId"]!.AllowDBNull = true;
        table.Columns["Port"]!.AllowDBNull = true;
        foreach (ParkingDevice x in values)
            table.Rows.Add(x.DeviceId, Db(x.LaneId), x.DeviceNumber, x.DeviceType, x.DeviceName,
                Db(x.IpAddress), Db(x.Port), x.Enabled);
        AddPage("장치", table, true,
            async row => await _client.SaveDeviceAsync(new ParkingDevice(
                LongValue(row, "DeviceId"), _siteId, NullableLong(row, "LaneId"),
                IntValue(row, "DeviceNumber"), TextValue(row, "DeviceType"),
                TextValue(row, "DeviceName"), NullableText(row, "IpAddress"),
                BoolValue(row, "Enabled"), NullableInt(row, "Port")), CancellationToken.None),
            async row => await _client.DeleteDeviceAsync(LongValue(row, "DeviceId"), CancellationToken.None));
    }

    private void AddLinkPage(IReadOnlyList<ParkingDeviceLink> values)
    {
        DataTable table = CreateTable(
            ("SourceDeviceId", typeof(long)), ("TargetDeviceId", typeof(long)),
            ("LinkType", typeof(string)), ("Enabled", typeof(bool)));
        foreach (ParkingDeviceLink x in values)
            table.Rows.Add(x.SourceDeviceId, x.TargetDeviceId, x.LinkType, x.Enabled);
        AddPage("장치 연결", table, true,
            async row => await _client.SaveDeviceLinkAsync(new ParkingDeviceLink(
                _siteId, LongValue(row, "SourceDeviceId"), LongValue(row, "TargetDeviceId"),
                TextValue(row, "LinkType"), BoolValue(row, "Enabled")), CancellationToken.None),
            async row => await _client.DeleteDeviceLinkAsync(
                LongValue(row, "SourceDeviceId"), LongValue(row, "TargetDeviceId"),
                TextValue(row, "LinkType"), CancellationToken.None));
    }

    private void AddVariablePage(IReadOnlyList<ParkingOperationVariable> values)
    {
        DataTable table = CreateTable(
            ("Groupnum", typeof(int)), ("CommandType", typeof(string)), ("Value", typeof(string)));
        table.Columns["Value"]!.AllowDBNull = true;
        foreach (ParkingOperationVariable x in values)
            table.Rows.Add(x.Groupnum, x.CommandType, Db(x.Value));
        AddPage("운영변수", table, true,
            async row => await _client.SaveOperationVariableAsync(new ParkingOperationVariable(
                IntValue(row, "Groupnum"), TextValue(row, "CommandType"),
                NullableText(row, "Value")), CancellationToken.None),
            async row => await _client.DeleteOperationVariableAsync(
                IntValue(row, "Groupnum"), TextValue(row, "CommandType"), CancellationToken.None));
    }

    private void AddPage(
        string title,
        DataTable table,
        bool allowAddDelete,
        Func<DataRow, Task> save,
        Func<DataRow, Task>? delete)
    {
        DataGridView grid = new()
        {
            Dock = DockStyle.Fill,
            DataSource = table,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (ConfigurationEditPolicy.ShouldCommit(
                    grid.IsCurrentCellDirty,
                    grid.CurrentCell is DataGridViewCheckBoxCell))
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(5)
        };
        Button saveButton = new() { Text = "저장", AutoSize = true };
        saveButton.Click += async (_, _) => await ExecuteAsync(grid, save, false);
        buttons.Controls.Add(saveButton);
        if (allowAddDelete)
        {
            Button deleteButton = new() { Text = "삭제", AutoSize = true };
            deleteButton.Click += async (_, _) =>
            {
                if (delete is not null) await ExecuteAsync(grid, delete, true);
            };
            Button addButton = new() { Text = "추가", AutoSize = true };
            addButton.Click += (_, _) =>
            {
                DataRow row = table.NewRow();
                foreach (DataColumn column in table.Columns)
                    if (column.DataType == typeof(bool)) row[column] = true;
                    else if (column.DataType == typeof(string)) row[column] = "";
                    else if (!column.AllowDBNull) row[column] = 0;
                table.Rows.Add(row);
                grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[0];
                grid.BeginEdit(true);
            };
            buttons.Controls.Add(deleteButton);
            buttons.Controls.Add(addButton);
        }
        Panel panel = new() { Dock = DockStyle.Fill };
        panel.Controls.Add(grid);
        panel.Controls.Add(buttons);
        TabPage page = new(title);
        page.Controls.Add(panel);
        _tabs.TabPages.Add(page);
    }

    private static async Task ExecuteAsync(
        DataGridView grid,
        Func<DataRow, Task> action,
        bool removeAfterSuccess)
    {
        if (grid.CurrentRow?.DataBoundItem is not DataRowView view) return;
        if (ConfigurationEditPolicy.ShouldCommit(
                grid.IsCurrentCellDirty,
                grid.CurrentCell is DataGridViewCheckBoxCell))
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        grid.EndEdit();
        view.EndEdit();
        try
        {
            await action(view.Row);
            if (removeAfterSuccess) view.Row.Delete();
            MessageBox.Show("저장되었습니다.", "현장 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"처리하지 못했습니다.\r\n{exception.Message}", "현장 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DataTable CreateTable(params (string Name, Type Type)[] columns)
    {
        DataTable table = new();
        foreach ((string name, Type type) in columns) table.Columns.Add(name, type);
        return table;
    }

    private static object Db(object? value) => value ?? DBNull.Value;
    private static long LongValue(DataRow row, string name) => Convert.ToInt64(row[name]);
    private static int IntValue(DataRow row, string name) => Convert.ToInt32(row[name]);
    private static bool BoolValue(DataRow row, string name) => Convert.ToBoolean(row[name]);
    private static string TextValue(DataRow row, string name) => Convert.ToString(row[name])?.Trim() ?? "";
    private static long? NullableLong(DataRow row, string name) => row.IsNull(name) || TextValue(row, name) == "" ? null : LongValue(row, name);
    private static int? NullableInt(DataRow row, string name) => row.IsNull(name) || TextValue(row, name) == "" ? null : IntValue(row, name);
    private static string? NullableText(DataRow row, string name) => row.IsNull(name) || TextValue(row, name) == "" ? null : TextValue(row, name);
}
