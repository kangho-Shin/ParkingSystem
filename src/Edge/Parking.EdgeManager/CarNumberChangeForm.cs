namespace Parking.EdgeManager;

public sealed partial class CarNumberChangeForm : Form
{
    public CarNumberChangeForm()
    {
        InitializeComponent();
    }

    public CarNumberChangeForm(string currentCarNumber) : this()
    {
        _currentText.Text = currentCarNumber;
        _newText.Text = currentCarNumber;
        _newText.SelectAll();
    }

    public string CarNumber { get; private set; } = "";

    private void ConfirmButtonClick(object? sender, EventArgs e)
    {
        string value = _newText.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show("새 차량번호를 입력하세요.", "차량번호 변경",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        CarNumber = value;
        DialogResult = DialogResult.OK;
        Close();
    }
}
