namespace Parking.EdgeManager;

partial class CarNumberChangeForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _layout = null!;
    private TextBox _currentText = null!;
    private TextBox _newText = null!;
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
        _currentText = new TextBox();
        _newText = new TextBox();
        _buttons = new FlowLayoutPanel();
        _confirmButton = new Button();
        _cancelButton = new Button();
        SuspendLayout();

        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.RowCount = 3;
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _currentText.ReadOnly = true;
        AddRow(0, "현재 차량번호", _currentText);
        AddRow(1, "새 차량번호", _newText);

        _buttons.Dock = DockStyle.Fill;
        _buttons.FlowDirection = FlowDirection.RightToLeft;
        _confirmButton.Text = "변경";
        _confirmButton.AutoSize = true;
        _confirmButton.Click += ConfirmButtonClick;
        _cancelButton.Text = "취소";
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _buttons.Controls.Add(_confirmButton);
        _buttons.Controls.Add(_cancelButton);
        _layout.Controls.Add(_buttons, 0, 2);
        _layout.SetColumnSpan(_buttons, 2);

        AcceptButton = _confirmButton;
        CancelButton = _cancelButton;
        ClientSize = new Size(440, 155);
        Controls.Add(_layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "차량번호 변경";
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
