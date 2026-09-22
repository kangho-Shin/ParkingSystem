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
        _rootLayout = new TableLayoutPanel();
        _siteLabel = new Label();
        _siteNumber = new NumericUpDown();
        _groupLabel = new Label();
        _groupNumber = new NumericUpDown();
        _carTypeLabel = new Label();
        _carType = new ComboBox();
        _entryAtLabel = new Label();
        _entryAt = new DateTimePicker();
        _exitAtLabel = new Label();
        _exitAt = new DateTimePicker();
        _calculateButton = new Button();
        _result = new TextBox();
        _rootLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_siteNumber).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_groupNumber).BeginInit();
        SuspendLayout();
        // 
        // _rootLayout
        // 
        _rootLayout.ColumnCount = 2;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(16);
        _rootLayout.RowCount = 7;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.Size = new Size(664, 481);
        _rootLayout.TabIndex = 0;
        // 
        // _siteLabel
        // 
        _siteLabel.Anchor = AnchorStyles.Left;
        _siteLabel.AutoSize = true;
        _siteLabel.Location = new Point(19, 29);
        _siteLabel.Name = "_siteLabel";
        _siteLabel.Size = new Size(43, 15);
        _siteLabel.TabIndex = 0;
        _siteLabel.Text = "사이트";
        // 
        // _siteNumber
        // 
        _siteNumber.Location = new Point(149, 19);
        _siteNumber.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
        _siteNumber.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _siteNumber.Name = "_siteNumber";
        _siteNumber.Size = new Size(160, 23);
        _siteNumber.TabIndex = 1;
        _siteNumber.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _groupLabel
        // 
        _groupLabel.Anchor = AnchorStyles.Left;
        _groupLabel.AutoSize = true;
        _groupLabel.Location = new Point(19, 71);
        _groupLabel.Name = "_groupLabel";
        _groupLabel.Size = new Size(31, 15);
        _groupLabel.TabIndex = 2;
        _groupLabel.Text = "그룹";
        // 
        // _groupNumber
        // 
        _groupNumber.Location = new Point(149, 61);
        _groupNumber.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
        _groupNumber.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _groupNumber.Name = "_groupNumber";
        _groupNumber.Size = new Size(160, 23);
        _groupNumber.TabIndex = 3;
        _groupNumber.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _carTypeLabel
        // 
        _carTypeLabel.Anchor = AnchorStyles.Left;
        _carTypeLabel.AutoSize = true;
        _carTypeLabel.Location = new Point(19, 113);
        _carTypeLabel.Name = "_carTypeLabel";
        _carTypeLabel.Size = new Size(31, 15);
        _carTypeLabel.TabIndex = 4;
        _carTypeLabel.Text = "차종";
        // 
        // _carType
        // 
        _carType.DropDownStyle = ComboBoxStyle.DropDownList;
        _carType.Location = new Point(149, 103);
        _carType.Name = "_carType";
        _carType.Size = new Size(160, 23);
        _carType.TabIndex = 5;
        // 
        // _entryAtLabel
        // 
        _entryAtLabel.Anchor = AnchorStyles.Left;
        _entryAtLabel.AutoSize = true;
        _entryAtLabel.Location = new Point(19, 155);
        _entryAtLabel.Name = "_entryAtLabel";
        _entryAtLabel.Size = new Size(55, 15);
        _entryAtLabel.TabIndex = 6;
        _entryAtLabel.Text = "입차시각";
        // 
        // _entryAt
        // 
        _entryAt.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _entryAt.Format = DateTimePickerFormat.Custom;
        _entryAt.Location = new Point(149, 145);
        _entryAt.Name = "_entryAt";
        _entryAt.Size = new Size(220, 23);
        _entryAt.TabIndex = 7;
        // 
        // _exitAtLabel
        // 
        _exitAtLabel.Anchor = AnchorStyles.Left;
        _exitAtLabel.AutoSize = true;
        _exitAtLabel.Location = new Point(19, 197);
        _exitAtLabel.Name = "_exitAtLabel";
        _exitAtLabel.Size = new Size(55, 15);
        _exitAtLabel.TabIndex = 8;
        _exitAtLabel.Text = "출차시각";
        // 
        // _exitAt
        // 
        _exitAt.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _exitAt.Format = DateTimePickerFormat.Custom;
        _exitAt.Location = new Point(149, 187);
        _exitAt.Name = "_exitAt";
        _exitAt.Size = new Size(220, 23);
        _exitAt.TabIndex = 9;
        // 
        // _calculateButton
        // 
        _calculateButton.AutoSize = true;
        _calculateButton.Location = new Point(149, 229);
        _calculateButton.Name = "_calculateButton";
        _calculateButton.Size = new Size(220, 36);
        _calculateButton.TabIndex = 10;
        _calculateButton.Text = "요금 계산";
        _calculateButton.Click += CalculateButtonClick;
        // 
        // _result
        // 
        _rootLayout.SetColumnSpan(_result, 2);
        _result.Dock = DockStyle.Fill;
        _result.Location = new Point(19, 271);
        _result.Multiline = true;
        _result.Name = "_result";
        _result.ReadOnly = true;
        _result.ScrollBars = ScrollBars.Vertical;
        _result.Size = new Size(626, 191);
        _result.TabIndex = 11;
        // 
        // MainForm
        // 
        ClientSize = new Size(664, 481);
        Controls.Add(_rootLayout);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Parking Fee Tester";
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_siteNumber).EndInit();
        ((System.ComponentModel.ISupportInitialize)_groupNumber).EndInit();
        ResumeLayout(false);
    }

}
