namespace Parking.Operator;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel _rootLayout = null!;
    private FlowLayoutPanel _deviceLayout = null!;
    private FlowLayoutPanel _searchLayout = null!;
    private FlowLayoutPanel _actionLayout = null!;
    private SplitContainer _contentSplit = null!;
    private Label _status = null!;
    private Label _entryDeviceLabel = null!;
    private Label _exitDeviceLabel = null!;
    private Label _carNumberLabel = null!;
    private Label _groupnumLabel = null!;
    private Label _discountKeysLabel = null!;
    private Label _fee = null!;
    private TextBox _carNumber = null!;
    private TextBox _discountKeys = null!;
    private NumericUpDown _groupnum = null!;
    private ComboBox _entryDevice = null!;
    private ComboBox _exitDevice = null!;
    private DataGridView _candidates = null!;
    private PictureBox _inImage = null!;
    private Button _searchButton = null!;
    private Button _quoteButton = null!;
    private Button _cardButton = null!;
    private Button _cashButton = null!;
    private Button _correctButton = null!;
    private Button _manualEntryButton = null!;
    private Button _manualExitButton = null!;
    private Button _openBarrierButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _rootLayout = new TableLayoutPanel();
        _deviceLayout = new FlowLayoutPanel();
        _searchLayout = new FlowLayoutPanel();
        _actionLayout = new FlowLayoutPanel();
        _contentSplit = new SplitContainer();
        _status = new Label();
        _entryDeviceLabel = new Label();
        _exitDeviceLabel = new Label();
        _carNumberLabel = new Label();
        _groupnumLabel = new Label();
        _discountKeysLabel = new Label();
        _fee = new Label();
        _carNumber = new TextBox();
        _discountKeys = new TextBox();
        _groupnum = new NumericUpDown();
        _entryDevice = new ComboBox();
        _exitDevice = new ComboBox();
        _candidates = new DataGridView();
        _inImage = new PictureBox();
        _searchButton = new Button();
        _quoteButton = new Button();
        _cardButton = new Button();
        _cashButton = new Button();
        _correctButton = new Button();
        _manualEntryButton = new Button();
        _manualExitButton = new Button();
        _openBarrierButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_contentSplit).BeginInit();
        _contentSplit.Panel1.SuspendLayout();
        _contentSplit.Panel2.SuspendLayout();
        _contentSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_groupnum).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_candidates).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_inImage).BeginInit();
        SuspendLayout();

        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Padding = new Padding(10);
        _rootLayout.RowCount = 4;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105F));

        _deviceLayout.Dock = DockStyle.Fill;
        _status.AutoSize = true;
        _status.Text = "연결 확인 중";
        _entryDeviceLabel.AutoSize = true;
        _entryDeviceLabel.Text = "입차장비";
        _exitDeviceLabel.AutoSize = true;
        _exitDeviceLabel.Text = "출차장비";
        _entryDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        _entryDevice.Width = 260;
        _exitDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        _exitDevice.Width = 260;
        _deviceLayout.Controls.AddRange(new Control[] { _status, _entryDeviceLabel, _entryDevice, _exitDeviceLabel, _exitDevice });

        _searchLayout.Dock = DockStyle.Fill;
        _carNumberLabel.AutoSize = true;
        _carNumberLabel.Text = "차량번호";
        _groupnumLabel.AutoSize = true;
        _groupnumLabel.Text = "그룹";
        _discountKeysLabel.AutoSize = true;
        _discountKeysLabel.Text = "할인키";
        _carNumber.Width = 150;
        _groupnum.Minimum = 1;
        _groupnum.Maximum = 999;
        _groupnum.Value = 1;
        _groupnum.Width = 70;
        _discountKeys.PlaceholderText = "예: 1,3";
        _discountKeys.Width = 130;
        _searchButton.AutoSize = true;
        _searchButton.Height = 38;
        _searchButton.Text = "차량조회";
        _quoteButton.AutoSize = true;
        _quoteButton.Height = 38;
        _quoteButton.Text = "할인·요금계산";
        _searchButton.Click += SearchButtonClick;
        _quoteButton.Click += QuoteButtonClick;
        _fee.AutoSize = true;
        _fee.Font = new Font("맑은 고딕", 13F, FontStyle.Bold);
        _searchLayout.Controls.AddRange(new Control[] { _carNumberLabel, _carNumber, _groupnumLabel, _groupnum, _searchButton, _discountKeysLabel, _discountKeys, _quoteButton, _fee });

        _contentSplit.Dock = DockStyle.Fill;
        _contentSplit.SplitterDistance = 750;
        _candidates.AutoGenerateColumns = true;
        _candidates.Dock = DockStyle.Fill;
        _candidates.ReadOnly = true;
        _candidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _candidates.SelectionChanged += CandidatesSelectionChanged;
        _inImage.BorderStyle = BorderStyle.FixedSingle;
        _inImage.Dock = DockStyle.Fill;
        _inImage.SizeMode = PictureBoxSizeMode.Zoom;
        _contentSplit.Panel1.Controls.Add(_candidates);
        _contentSplit.Panel2.Controls.Add(_inImage);

        _actionLayout.AutoScroll = true;
        _actionLayout.Dock = DockStyle.Fill;
        _cardButton.AutoSize = true;
        _cardButton.Height = 38;
        _cardButton.Text = "카드결제";
        _cashButton.AutoSize = true;
        _cashButton.Height = 38;
        _cashButton.Text = "현금결제";
        _correctButton.AutoSize = true;
        _correctButton.Height = 38;
        _correctButton.Text = "차량번호 수정";
        _manualEntryButton.AutoSize = true;
        _manualEntryButton.Height = 38;
        _manualEntryButton.Text = "수동 입차";
        _manualExitButton.AutoSize = true;
        _manualExitButton.Height = 38;
        _manualExitButton.Text = "수동 출차";
        _openBarrierButton.AutoSize = true;
        _openBarrierButton.Height = 38;
        _openBarrierButton.Text = "차단기 개방";
        _cardButton.Click += CardButtonClick;
        _cashButton.Click += CashButtonClick;
        _correctButton.Click += CorrectButtonClick;
        _manualEntryButton.Click += ManualEntryButtonClick;
        _manualExitButton.Click += ManualExitButtonClick;
        _openBarrierButton.Click += OpenBarrierButtonClick;
        _actionLayout.Controls.AddRange(new Control[] { _cardButton, _cashButton, _correctButton, _manualEntryButton, _manualExitButton, _openBarrierButton });

        _rootLayout.Controls.Add(_deviceLayout, 0, 0);
        _rootLayout.Controls.Add(_searchLayout, 0, 1);
        _rootLayout.Controls.Add(_contentSplit, 0, 2);
        _rootLayout.Controls.Add(_actionLayout, 0, 3);
        Controls.Add(_rootLayout);
        ClientSize = new Size(1234, 721);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Parking Operator";
        FormClosed += MainFormFormClosed;
        Shown += MainFormShown;

        _contentSplit.Panel1.ResumeLayout(false);
        _contentSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_contentSplit).EndInit();
        _contentSplit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_groupnum).EndInit();
        ((System.ComponentModel.ISupportInitialize)_candidates).EndInit();
        ((System.ComponentModel.ISupportInitialize)_inImage).EndInit();
        ResumeLayout(false);
    }

}
