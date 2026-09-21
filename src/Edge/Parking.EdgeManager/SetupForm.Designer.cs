namespace Parking.EdgeManager;

partial class SetupForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _layout = null!;
    private FlowLayoutPanel _buttonLayout = null!;
    private Label _siteIdLabel = null!;
    private Label _centralServerUrlLabel = null!;
    private Label _imageServerUrlLabel = null!;
    private Label _siteAuthKeyLabel = null!;
    private Label _imageWatchPathLabel = null!;
    private NumericUpDown _siteId = null!;
    private TextBox _centralServerUrl = null!;
    private TextBox _imageServerUrl = null!;
    private TextBox _siteAuthKey = null!;
    private TextBox _imageWatchPath = null!;
    private Button _saveButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _layout = new TableLayoutPanel();
        _buttonLayout = new FlowLayoutPanel();
        _siteIdLabel = new Label();
        _centralServerUrlLabel = new Label();
        _imageServerUrlLabel = new Label();
        _siteAuthKeyLabel = new Label();
        _imageWatchPathLabel = new Label();
        _siteId = new NumericUpDown();
        _centralServerUrl = new TextBox();
        _imageServerUrl = new TextBox();
        _siteAuthKey = new TextBox();
        _imageWatchPath = new TextBox();
        _saveButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_siteId).BeginInit();
        SuspendLayout();

        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(16);
        _layout.RowCount = 6;
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));

        _siteIdLabel.Anchor = AnchorStyles.Left;
        _siteIdLabel.AutoSize = true;
        _siteIdLabel.Text = "현장번호(sitenum)";
        _centralServerUrlLabel.Anchor = AnchorStyles.Left;
        _centralServerUrlLabel.AutoSize = true;
        _centralServerUrlLabel.Text = "중앙 서버 주소";
        _imageServerUrlLabel.Anchor = AnchorStyles.Left;
        _imageServerUrlLabel.AutoSize = true;
        _imageServerUrlLabel.Text = "이미지 서버 주소";
        _siteAuthKeyLabel.Anchor = AnchorStyles.Left;
        _siteAuthKeyLabel.AutoSize = true;
        _siteAuthKeyLabel.Text = "현장 인증키";
        _imageWatchPathLabel.Anchor = AnchorStyles.Left;
        _imageWatchPathLabel.AutoSize = true;
        _imageWatchPathLabel.Text = "영상 감시폴더";
        _siteId.Dock = DockStyle.Fill;
        _siteId.Minimum = 1;
        _siteId.Maximum = 999999999;
        _centralServerUrl.Dock = DockStyle.Fill;
        _centralServerUrl.Text = "http://localhost:5100/";
        _imageServerUrl.Dock = DockStyle.Fill;
        _imageServerUrl.Text = "http://localhost:5400/";
        _siteAuthKey.Dock = DockStyle.Fill;
        _siteAuthKey.UseSystemPasswordChar = true;
        _imageWatchPath.Dock = DockStyle.Fill;
        _imageWatchPath.Text = @"D:\LPR\IMAGE";
        _saveButton.AutoSize = true;
        _saveButton.Text = "저장";
        _saveButton.Click += SaveButtonClick;
        _buttonLayout.Dock = DockStyle.Fill;
        _buttonLayout.FlowDirection = FlowDirection.RightToLeft;
        _buttonLayout.Controls.Add(_saveButton);

        _layout.Controls.Add(_siteIdLabel, 0, 0);
        _layout.Controls.Add(_siteId, 1, 0);
        _layout.Controls.Add(_centralServerUrlLabel, 0, 1);
        _layout.Controls.Add(_centralServerUrl, 1, 1);
        _layout.Controls.Add(_imageServerUrlLabel, 0, 2);
        _layout.Controls.Add(_imageServerUrl, 1, 2);
        _layout.Controls.Add(_siteAuthKeyLabel, 0, 3);
        _layout.Controls.Add(_siteAuthKey, 1, 3);
        _layout.Controls.Add(_imageWatchPathLabel, 0, 4);
        _layout.Controls.Add(_imageWatchPath, 1, 4);
        _layout.Controls.Add(_buttonLayout, 0, 5);
        _layout.SetColumnSpan(_buttonLayout, 2);
        Controls.Add(_layout);
        AcceptButton = _saveButton;
        ClientSize = new Size(604, 321);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "현장 최초 설정";

        ((System.ComponentModel.ISupportInitialize)_siteId).EndInit();
        ResumeLayout(false);
    }

}
