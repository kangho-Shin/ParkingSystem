namespace Parking.FeeTester;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _rootLayout = null!;
    private Label _siteLabel = null!;
    private Label _groupLabel = null!;
    private Label _carTypeLabel = null!;
    private Label _entryAtLabel = null!;
    private Label _exitAtLabel = null!;
    private NumericUpDown _siteNumber = null!;
    private NumericUpDown _groupNumber = null!;
    private ComboBox _carType = null!;
    private DateTimePicker _entryAt = null!;
    private DateTimePicker _exitAt = null!;
    private Button _calculateButton = null!;
    private TextBox _result = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _rootLayout = new TableLayoutPanel();
        _siteLabel = new Label();
        _groupLabel = new Label();
        _carTypeLabel = new Label();
        _entryAtLabel = new Label();
        _exitAtLabel = new Label();
        _siteNumber = new NumericUpDown();
        _groupNumber = new NumericUpDown();
        _carType = new ComboBox();
        _entryAt = new DateTimePicker();
        _exitAt = new DateTimePicker();
        _calculateButton = new Button();
        _result = new TextBox();
        ((System.ComponentModel.ISupportInitialize)_siteNumber).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_groupNumber).BeginInit();
        SuspendLayout();

        _rootLayout.ColumnCount = 2;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Padding = new Padding(16);
        _rootLayout.RowCount = 7;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _siteLabel.Anchor = AnchorStyles.Left;
        _siteLabel.AutoSize = true;
        _siteLabel.Text = "사이트";
        _groupLabel.Anchor = AnchorStyles.Left;
        _groupLabel.AutoSize = true;
        _groupLabel.Text = "그룹";
        _carTypeLabel.Anchor = AnchorStyles.Left;
        _carTypeLabel.AutoSize = true;
        _carTypeLabel.Text = "차종";
        _entryAtLabel.Anchor = AnchorStyles.Left;
        _entryAtLabel.AutoSize = true;
        _entryAtLabel.Text = "입차시각";
        _exitAtLabel.Anchor = AnchorStyles.Left;
        _exitAtLabel.AutoSize = true;
        _exitAtLabel.Text = "출차시각";

        _siteNumber.Minimum = 1;
        _siteNumber.Maximum = 9999;
        _siteNumber.Value = 1;
        _siteNumber.Width = 160;
        _groupNumber.Minimum = 1;
        _groupNumber.Maximum = 999;
        _groupNumber.Value = 1;
        _groupNumber.Width = 160;
        _carType.DropDownStyle = ComboBoxStyle.DropDownList;
        _carType.Width = 160;
        _entryAt.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _entryAt.Format = DateTimePickerFormat.Custom;
        _entryAt.Width = 220;
        _exitAt.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _exitAt.Format = DateTimePickerFormat.Custom;
        _exitAt.Width = 220;
        _calculateButton.AutoSize = true;
        _calculateButton.Text = "요금 계산";
        _calculateButton.Click += CalculateButtonClick;
        _result.Dock = DockStyle.Fill;
        _result.Multiline = true;
        _result.ReadOnly = true;
        _result.ScrollBars = ScrollBars.Vertical;

        _rootLayout.Controls.Add(_siteLabel, 0, 0);
        _rootLayout.Controls.Add(_siteNumber, 1, 0);
        _rootLayout.Controls.Add(_groupLabel, 0, 1);
        _rootLayout.Controls.Add(_groupNumber, 1, 1);
        _rootLayout.Controls.Add(_carTypeLabel, 0, 2);
        _rootLayout.Controls.Add(_carType, 1, 2);
        _rootLayout.Controls.Add(_entryAtLabel, 0, 3);
        _rootLayout.Controls.Add(_entryAt, 1, 3);
        _rootLayout.Controls.Add(_exitAtLabel, 0, 4);
        _rootLayout.Controls.Add(_exitAt, 1, 4);
        _rootLayout.Controls.Add(_calculateButton, 1, 5);
        _rootLayout.Controls.Add(_result, 0, 6);
        _rootLayout.SetColumnSpan(_result, 2);
        Controls.Add(_rootLayout);
        ClientSize = new Size(664, 481);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Parking Fee Tester";

        ((System.ComponentModel.ISupportInitialize)_siteNumber).EndInit();
        ((System.ComponentModel.ISupportInitialize)_groupNumber).EndInit();
        ResumeLayout(false);
    }

}
