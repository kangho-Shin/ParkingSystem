namespace Parking.EdgeManager;

partial class EntryVehicleForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _root = null!;
    private FlowLayoutPanel _filters = null!;
    private SplitContainer _content = null!;
    private DataGridView _grid = null!;
    private Panel _picturePanel = null!;
    private PictureBox _picture = null!;
    private Label _imageStatus = null!;
    private FlowLayoutPanel _footer = null!;
    private CheckBox _useFrom = null!;
    private DateTimePicker _fromDate = null!;
    private CheckBox _useTo = null!;
    private DateTimePicker _toDate = null!;
    private ComboBox _groupCombo = null!;
    private ComboBox _deviceCombo = null!;
    private TextBox _carNumberText = null!;
    private Button _searchButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Label _pageLabel = null!;
    private Button _manualEntryButton = null!;
    private Button _changeCarNumberButton = null!;
    private Button _closeButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _root = new TableLayoutPanel();
        _filters = new FlowLayoutPanel();
        _content = new SplitContainer();
        _grid = new DataGridView();
        _picturePanel = new Panel();
        _picture = new PictureBox();
        _imageStatus = new Label();
        _footer = new FlowLayoutPanel();
        _useFrom = new CheckBox();
        _fromDate = new DateTimePicker();
        _useTo = new CheckBox();
        _toDate = new DateTimePicker();
        _groupCombo = new ComboBox();
        _deviceCombo = new ComboBox();
        _carNumberText = new TextBox();
        _searchButton = new Button();
        _previousButton = new Button();
        _nextButton = new Button();
        _pageLabel = new Label();
        _manualEntryButton = new Button();
        _changeCarNumberButton = new Button();
        _closeButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_content).BeginInit();
        _content.Panel1.SuspendLayout();
        _content.Panel2.SuspendLayout();
        _content.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_picture).BeginInit();
        SuspendLayout();

        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(8);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        _filters.Dock = DockStyle.Fill;
        _filters.AutoScroll = true;
        AddFilter(_useFrom, "시작일 사용");
        SetupDate(_fromDate);
        AddFilter(_fromDate);
        AddFilter(_useTo, "종료일 사용");
        SetupDate(_toDate);
        AddFilter(_toDate);
        SetupCombo(_groupCombo, 120);
        AddLabeledFilter("그룹", _groupCombo);
        SetupCombo(_deviceCombo, 190);
        AddLabeledFilter("장치", _deviceCombo);
        _carNumberText.Width = 110;
        AddLabeledFilter("차량번호", _carNumberText);
        _searchButton.Text = "조회";
        _searchButton.AutoSize = true;
        _searchButton.Click += SearchButtonClick;
        AddFilter(_searchButton);

        _content.Dock = DockStyle.Fill;
        _content.FixedPanel = FixedPanel.Panel2;
        _content.SplitterDistance = 900;
        _content.Panel1.Controls.Add(_grid);
        _content.Panel2.Controls.Add(_picturePanel);
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _grid.Dock = DockStyle.Fill;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.SelectionChanged += GridSelectionChanged;
        _picturePanel.Dock = DockStyle.Fill;
        _picturePanel.Controls.Add(_picture);
        _picturePanel.Controls.Add(_imageStatus);
        _picture.BackColor = Color.Black;
        _picture.Dock = DockStyle.Fill;
        _picture.SizeMode = PictureBoxSizeMode.Zoom;
        _imageStatus.BackColor = Color.Transparent;
        _imageStatus.Dock = DockStyle.Bottom;
        _imageStatus.Height = 28;
        _imageStatus.Text = "입차 사진 없음";
        _imageStatus.TextAlign = ContentAlignment.MiddleCenter;

        _footer.Dock = DockStyle.Fill;
        _footer.FlowDirection = FlowDirection.LeftToRight;
        _previousButton.Text = "이전";
        _previousButton.Click += PreviousButtonClick;
        _nextButton.Text = "다음";
        _nextButton.Click += NextButtonClick;
        _pageLabel.AutoSize = true;
        _pageLabel.Margin = new Padding(12, 9, 20, 0);
        _pageLabel.Text = "총 0건";
        _manualEntryButton.Text = "수동입차";
        _manualEntryButton.AutoSize = true;
        _manualEntryButton.Click += ManualEntryButtonClick;
        _changeCarNumberButton.Text = "차량번호 변경";
        _changeCarNumberButton.AutoSize = true;
        _changeCarNumberButton.Click += ChangeCarNumberButtonClick;
        _closeButton.Text = "닫기";
        _closeButton.AutoSize = true;
        _closeButton.Click += (_, _) => Close();
        _footer.Controls.AddRange(new Control[] { _previousButton, _nextButton, _pageLabel, _manualEntryButton, _changeCarNumberButton, _closeButton });

        _root.Controls.Add(_filters, 0, 0);
        _root.Controls.Add(_content, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        ClientSize = new Size(1280, 720);
        Controls.Add(_root);
        MinimumSize = new Size(1050, 600);
        StartPosition = FormStartPosition.CenterParent;
        Text = "입차차량 관리";
        FormClosed += EntryVehicleFormFormClosed;
        Shown += EntryVehicleFormShown;

        _content.Panel1.ResumeLayout(false);
        _content.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_content).EndInit();
        _content.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_picture).EndInit();
        ResumeLayout(false);
    }

    private void AddFilter(Control control, string? text = null)
    {
        if (text is not null) control.Text = text;
        control.Margin = new Padding(6, 8, 3, 3);
        _filters.Controls.Add(control);
    }

    private void AddLabeledFilter(string label, Control control)
    {
        AddFilter(new Label { Text = label, AutoSize = true, Margin = new Padding(10, 13, 0, 0) });
        AddFilter(control);
    }

    private static void SetupDate(DateTimePicker picker)
    {
        picker.CustomFormat = "yyyy-MM-dd HH:mm";
        picker.Format = DateTimePickerFormat.Custom;
        picker.Width = 145;
    }

    private static void SetupCombo(ComboBox combo, int width)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Width = width;
    }
}
