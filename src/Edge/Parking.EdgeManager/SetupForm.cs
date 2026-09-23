using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

public sealed partial class SetupForm : Form
{
    private readonly IEdgeManagementClient _client;

    public SetupForm()
    {
        InitializeComponent();
        _client = null!;
    }

    public SetupForm(IEdgeManagementClient client, EdgeSetupResponse? current = null) : this()
    {
        _client = client;

        if (current is not null)
        {
            _siteId.Value = current.SiteId;
            _centralServerUrl.Text = current.CentralServerUrl;
            _parkingApiUrl.Text = current.ParkingApiUrl;
            _imageServerUrl.Text = current.ImageServerUrl;
            _imageWatchPath.Text = current.ImageWatchPath;
        }
    }

    private async void SaveButtonClick(object? sender, EventArgs e)
    {
        if (!IsHttpUrl(_centralServerUrl.Text) || !IsHttpUrl(_parkingApiUrl.Text) ||
            !IsHttpUrl(_imageServerUrl.Text))
        {
            MessageBox.Show("서버 주소는 http:// 또는 https:// 주소로 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_siteAuthKey.Text))
        {
            MessageBox.Show("현장 인증키를 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_imageWatchPath.Text))
        {
            MessageBox.Show("영상 감시폴더를 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _saveButton.Enabled = false;
        try
        {
            await _client.SaveSetupAsync(new EdgeSetupRequest(
                decimal.ToInt64(_siteId.Value),
                _centralServerUrl.Text.Trim(),
                _parkingApiUrl.Text.Trim(),
                _imageServerUrl.Text.Trim(),
                _imageWatchPath.Text.Trim(),
                _siteAuthKey.Text.Trim()), CancellationToken.None);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"설정을 저장하지 못했습니다.\r\n{exception.Message}", "설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _saveButton.Enabled = true; }
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

}
