using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private System.Windows.Forms.Timer _refreshTimer = null!;
    private TableLayoutPanel _rootLayout = null!;
    private FlowLayoutPanel _statusLayout = null!;
    private TableLayoutPanel _contentLayout = null!;
    private TableLayoutPanel _picturesLayout = null!;
    private GroupBox _entryGroup = null!;
    private GroupBox _activityGroup = null!;
    private GroupBox _inPictureGroup = null!;
    private GroupBox _outPictureGroup = null!;
    private GroupBox _configurationGroup = null!;
    private Label _serviceStatus = null!;
    private Label _gatewayStatus = null!;
    private Label _centralStatus = null!;
    private Label _syncStatus = null!;
    private Label _outboxStatus = null!;
    private Label _lprStatus = null!;
    private Button _configurationButton = null!;
    private Button _entryVehicleButton = null!;
    private Button _exitVehicleButton = null!;
    private DataGridView _entryGrid = null!;
    private DataGridView _activityGrid = null!;
    private PictureBox _inPicture = null!;
    private PictureBox _outPicture = null!;
    private TreeView _configurationTree = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _refreshTimer = new System.Windows.Forms.Timer(components);
        _rootLayout = new TableLayoutPanel();
        _statusLayout = new FlowLayoutPanel();
        _contentLayout = new TableLayoutPanel();
        _picturesLayout = new TableLayoutPanel();
        _entryGroup = new GroupBox();
        _activityGroup = new GroupBox();
        _inPictureGroup = new GroupBox();
        _outPictureGroup = new GroupBox();
        _configurationGroup = new GroupBox();
        _serviceStatus = new Label();
        _gatewayStatus = new Label();
        _centralStatus = new Label();
        _syncStatus = new Label();
        _outboxStatus = new Label();
        _lprStatus = new Label();
        _configurationButton = new Button();
        _entryVehicleButton = new Button();
        _exitVehicleButton = new Button();
        _entryGrid = new DataGridView();
        _activityGrid = new DataGridView();
        _inPicture = new PictureBox();
        _outPicture = new PictureBox();
        _configurationTree = new TreeView();
        ((System.ComponentModel.ISupportInitialize)_entryGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_activityGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_inPicture).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_outPicture).BeginInit();
        SuspendLayout();

        _refreshTimer.Interval = 1000;
        _refreshTimer.Tick += RefreshTimerTick;

        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Padding = new Padding(8);
        _rootLayout.RowCount = 3;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));

        _statusLayout.Dock = DockStyle.Fill;
        _statusLayout.Controls.AddRange(new Control[] { _serviceStatus, _gatewayStatus, _centralStatus, _syncStatus, _outboxStatus, _lprStatus, _entryVehicleButton, _exitVehicleButton, _configurationButton });

        _serviceStatus.AutoSize = true;
        _serviceStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _serviceStatus.Margin = new Padding(8, 10, 12, 0);
        _serviceStatus.Text = "EdgeService";
        _gatewayStatus.AutoSize = true;
        _gatewayStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _gatewayStatus.Margin = new Padding(8, 10, 12, 0);
        _gatewayStatus.Text = "Gateway";
        _centralStatus.AutoSize = true;
        _centralStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _centralStatus.Margin = new Padding(8, 10, 12, 0);
        _centralStatus.Text = "중앙 API";
        _syncStatus.AutoSize = true;
        _syncStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _syncStatus.Margin = new Padding(8, 10, 12, 0);
        _syncStatus.Text = "설정 동기화";
        _outboxStatus.AutoSize = true;
        _outboxStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _outboxStatus.Margin = new Padding(8, 10, 12, 0);
        _outboxStatus.Text = "전송 대기 0건";
        _lprStatus.AutoSize = true;
        _lprStatus.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
        _lprStatus.Margin = new Padding(8, 10, 12, 0);
        _lprStatus.Text = "LPR 접속 0대";
        _configurationButton.AutoSize = true;
        _configurationButton.Text = "환경설정";
        _configurationButton.Click += ConfigurationButtonClick;
        _entryVehicleButton.AutoSize = true;
        _entryVehicleButton.Text = "입차차량 관리";
        _entryVehicleButton.Click += EntryVehicleButtonClick;
        _exitVehicleButton.AutoSize = true;
        _exitVehicleButton.Text = "출차차량 조회";
        _exitVehicleButton.Click += ExitVehicleButtonClick;

        _contentLayout.ColumnCount = 3;
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        _contentLayout.Dock = DockStyle.Fill;
        _contentLayout.RowCount = 1;
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _entryGroup.Dock = DockStyle.Fill;
        _entryGroup.Padding = new Padding(6);
        _entryGroup.Text = "현재 입차 차량";
        _entryGroup.Controls.Add(_entryGrid);
        _entryGrid.AllowUserToAddRows = false;
        _entryGrid.AllowUserToDeleteRows = false;
        _entryGrid.AutoGenerateColumns = false;
        _entryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _entryGrid.Dock = DockStyle.Fill;
        _entryGrid.MultiSelect = false;
        _entryGrid.ReadOnly = true;
        _entryGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _entryGrid.SelectionChanged += EntryGridSelectionChanged;

        _activityGroup.Dock = DockStyle.Fill;
        _activityGroup.Padding = new Padding(6);
        _activityGroup.Text = "정산 · 출차 처리";
        _activityGroup.Controls.Add(_activityGrid);
        _activityGrid.AllowUserToAddRows = false;
        _activityGrid.AllowUserToDeleteRows = false;
        _activityGrid.AutoGenerateColumns = false;
        _activityGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _activityGrid.Dock = DockStyle.Fill;
        _activityGrid.MultiSelect = false;
        _activityGrid.ReadOnly = true;
        _activityGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _activityGrid.SelectionChanged += ActivityGridSelectionChanged;

        _picturesLayout.ColumnCount = 1;
        _picturesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _picturesLayout.Dock = DockStyle.Fill;
        _picturesLayout.RowCount = 2;
        _picturesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _picturesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _inPictureGroup.Dock = DockStyle.Fill;
        _inPictureGroup.Padding = new Padding(6);
        _inPictureGroup.Text = "입차 사진";
        _inPictureGroup.Controls.Add(_inPicture);
        _inPicture.BackColor = Color.Black;
        _inPicture.Dock = DockStyle.Fill;
        _inPicture.SizeMode = PictureBoxSizeMode.Zoom;

        _outPictureGroup.Dock = DockStyle.Fill;
        _outPictureGroup.Padding = new Padding(6);
        _outPictureGroup.Text = "출차 사진";
        _outPictureGroup.Controls.Add(_outPicture);
        _outPicture.BackColor = Color.Black;
        _outPicture.Dock = DockStyle.Fill;
        _outPicture.SizeMode = PictureBoxSizeMode.Zoom;

        _configurationGroup.Dock = DockStyle.Fill;
        _configurationGroup.Padding = new Padding(6);
        _configurationGroup.Text = "현장 설정";
        _configurationGroup.Controls.Add(_configurationTree);
        _configurationTree.Dock = DockStyle.Fill;

        _picturesLayout.Controls.Add(_inPictureGroup, 0, 0);
        _picturesLayout.Controls.Add(_outPictureGroup, 0, 1);
        _contentLayout.Controls.Add(_entryGroup, 0, 0);
        _contentLayout.Controls.Add(_activityGroup, 1, 0);
        _contentLayout.Controls.Add(_picturesLayout, 2, 0);
        _rootLayout.Controls.Add(_statusLayout, 0, 0);
        _rootLayout.Controls.Add(_contentLayout, 0, 1);
        _rootLayout.Controls.Add(_configurationGroup, 0, 2);
        Controls.Add(_rootLayout);

        MinimumSize = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Parking Edge Manager";
        ClientSize = new Size(1484, 861);
        FormClosed += MainFormFormClosed;
        Shown += MainFormShown;

        ((System.ComponentModel.ISupportInitialize)_entryGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_activityGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_inPicture).EndInit();
        ((System.ComponentModel.ISupportInitialize)_outPicture).EndInit();
        ResumeLayout(false);
    }

}
