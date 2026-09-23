namespace Parking.EdgeManager;

partial class ExitVehicleForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _root = null!;
    private FlowLayoutPanel _filters = null!;
    private SplitContainer _content = null!;
    private DataGridView _grid = null!;
    private TableLayoutPanel _pictures = null!;
    private PictureBox _inPicture = null!;
    private PictureBox _outPicture = null!;
    private Label _imageStatus = null!;
    private FlowLayoutPanel _footer = null!;
    private DateTimePicker _fromDate = null!;
    private DateTimePicker _toDate = null!;
    private ComboBox _statusCombo = null!;
    private ComboBox _groupCombo = null!;
    private ComboBox _deviceCombo = null!;
    private TextBox _carNumberText = null!;
    private Button _searchButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Label _pageLabel = null!;
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
        _pictures = new TableLayoutPanel();
        _inPicture = new PictureBox();
        _outPicture = new PictureBox();
        _imageStatus = new Label();
        _footer = new FlowLayoutPanel();
        _fromDate = new DateTimePicker();
        _toDate = new DateTimePicker();
        _statusCombo = new ComboBox();
        _groupCombo = new ComboBox();
        _deviceCombo = new ComboBox();
        _carNumberText = new TextBox();
        _searchButton = new Button();
        _previousButton = new Button();
        _nextButton = new Button();
        _pageLabel = new Label();
        _closeButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_content).BeginInit();
        _content.Panel1.SuspendLayout();
        _content.Panel2.SuspendLayout();
        _content.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_inPicture).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_outPicture).BeginInit();
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
        SetupDate(_fromDate);
        SetupDate(_toDate);
        AddLabeledFilter("시작", _fromDate);
        AddLabeledFilter("종료", _toDate);
        SetupCombo(_statusCombo, 105);
        AddLabeledFilter("상태", _statusCombo);
        SetupCombo(_groupCombo, 110);
        AddLabeledFilter("그룹", _groupCombo);
        SetupCombo(_deviceCombo, 180);
        AddLabeledFilter("처리장치", _deviceCombo);
        _carNumberText.Width = 105;
        AddLabeledFilter("차량번호", _carNumberText);
        _searchButton.Text = "조회";
        _searchButton.AutoSize = true;
        _searchButton.Click += SearchButtonClick;
        AddFilter(_searchButton);

        _content.Dock = DockStyle.Fill;
        _content.FixedPanel = FixedPanel.Panel2;
        _content.SplitterDistance = 900;
        _content.Panel1.Controls.Add(_grid);
        _content.Panel2.Controls.Add(_pictures);
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _grid.Dock = DockStyle.Fill;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.SelectionChanged += GridSelectionChanged;

        _pictures.ColumnCount = 1;
        _pictures.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _pictures.Dock = DockStyle.Fill;
        _pictures.RowCount = 3;
        _pictures.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _pictures.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _pictures.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        SetupPicture(_inPicture);
        SetupPicture(_outPicture);
        _imageStatus.Dock = DockStyle.Fill;
        _imageStatus.TextAlign = ContentAlignment.MiddleCenter;
        _pictures.Controls.Add(_inPicture, 0, 0);
        _pictures.Controls.Add(_outPicture, 0, 1);
        _pictures.Controls.Add(_imageStatus, 0, 2);

        _footer.Dock = DockStyle.Fill;
        _previousButton.Text = "이전";
        _previousButton.Click += PreviousButtonClick;
        _nextButton.Text = "다음";
        _nextButton.Click += NextButtonClick;
        _pageLabel.AutoSize = true;
        _pageLabel.Margin = new Padding(12, 9, 20, 0);
        _pageLabel.Text = "총 0건";
        _closeButton.Text = "닫기";
        _closeButton.AutoSize = true;
        _closeButton.Click += (_, _) => Close();
        _footer.Controls.AddRange(new Control[] { _previousButton, _nextButton, _pageLabel, _closeButton });

        _root.Controls.Add(_filters, 0, 0);
        _root.Controls.Add(_content, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        ClientSize = new Size(1320, 740);
        Controls.Add(_root);
        MinimumSize = new Size(1080, 620);
        StartPosition = FormStartPosition.CenterParent;
        Text = "출차차량 조회";
        FormClosed += ExitVehicleFormFormClosed;
        Shown += ExitVehicleFormShown;

        _content.Panel1.ResumeLayout(false);
        _content.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_content).EndInit();
        _content.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_inPicture).EndInit();
        ((System.ComponentModel.ISupportInitialize)_outPicture).EndInit();
        ResumeLayout(false);
    }

    private void AddFilter(Control control)
    {
        control.Margin = new Padding(6, 8, 3, 3);
        _filters.Controls.Add(control);
    }

    private void AddLabeledFilter(string label, Control control)
    {
        Label caption = new() { Text = label, AutoSize = true, Margin = new Padding(10, 13, 0, 0) };
        _filters.Controls.Add(caption);
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

    private static void SetupPicture(PictureBox picture)
    {
        picture.BackColor = Color.Black;
        picture.Dock = DockStyle.Fill;
        picture.Margin = new Padding(3);
        picture.SizeMode = PictureBoxSizeMode.Zoom;
    }
}
