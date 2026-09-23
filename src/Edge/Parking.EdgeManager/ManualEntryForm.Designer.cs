namespace Parking.EdgeManager;

partial class ManualEntryForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _layout = null!;
    private ComboBox _laneCombo = null!;
    private ComboBox _deviceCombo = null!;
    private TextBox _carNumberText = null!;
    private DateTimePicker _inDateTime = null!;
    private NumericUpDown _carTypeNumber = null!;
    private FlowLayoutPanel _buttons = null!;
    private Button _confirmButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _layout = new TableLayoutPanel();
        _laneCombo = new ComboBox();
        _deviceCombo = new ComboBox();
        _carNumberText = new TextBox();
        _inDateTime = new DateTimePicker();
        _carTypeNumber = new NumericUpDown();
        _buttons = new FlowLayoutPanel();
        _confirmButton = new Button();
        _cancelButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_carTypeNumber).BeginInit();
        SuspendLayout();

        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.RowCount = 6;
        for (int i = 0; i < 5; i++) _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        AddRow(0, "입차 차로", _laneCombo);
        AddRow(1, "LPR 장치", _deviceCombo);
        AddRow(2, "차량번호", _carNumberText);
        AddRow(3, "입차시간", _inDateTime);
        AddRow(4, "차종", _carTypeNumber);

        _laneCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _laneCombo.SelectedIndexChanged += LaneComboSelectedIndexChanged;
        _deviceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _inDateTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _inDateTime.Format = DateTimePickerFormat.Custom;
        _carTypeNumber.Minimum = 1;
        _carTypeNumber.Maximum = 3;
        _carTypeNumber.Value = 1;

        _buttons.Dock = DockStyle.Fill;
        _buttons.FlowDirection = FlowDirection.RightToLeft;
        _confirmButton.Text = "입차 처리";
        _confirmButton.AutoSize = true;
        _confirmButton.Click += ConfirmButtonClick;
        _cancelButton.Text = "취소";
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _buttons.Controls.Add(_confirmButton);
        _buttons.Controls.Add(_cancelButton);
        _layout.Controls.Add(_buttons, 0, 5);
        _layout.SetColumnSpan(_buttons, 2);

        AcceptButton = _confirmButton;
        CancelButton = _cancelButton;
        ClientSize = new Size(460, 260);
        Controls.Add(_layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "수동입차";
        ((System.ComponentModel.ISupportInitialize)_carTypeNumber).EndInit();
        ResumeLayout(false);
    }

    private void AddRow(int row, string labelText, Control control)
    {
        Label label = new() { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left };
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 5, 3, 5);
        _layout.Controls.Add(label, 0, row);
        _layout.Controls.Add(control, 1, row);
    }
}
