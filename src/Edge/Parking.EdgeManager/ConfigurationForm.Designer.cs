namespace Parking.EdgeManager;

partial class ConfigurationForm
{
    private System.ComponentModel.IContainer? components;
    private TabControl _tabs = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _tabs = new TabControl();
        SuspendLayout();
        _tabs.Dock = DockStyle.Fill;
        _tabs.Location = new Point(0, 0);
        _tabs.Name = "_tabs";
        _tabs.SelectedIndex = 0;
        _tabs.Size = new Size(1034, 611);
        Controls.Add(_tabs);
        ClientSize = new Size(1034, 611);
        StartPosition = FormStartPosition.CenterParent;
        Text = "현장 설정";
        ResumeLayout(false);
    }
}
