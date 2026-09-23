using Newtonsoft.Json.Linq;
using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        string baseUrl = LoadBaseUrl();
        using HttpClient httpClient = new()
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
        EdgeManagementClient client = new(httpClient);
        EdgeSetupResponse? setup;
        try { setup = client.GetSetupAsync(CancellationToken.None).GetAwaiter().GetResult(); }
        catch (Exception exception)
        {
            MessageBox.Show($"EdgeService에 연결할 수 없습니다.\r\n{exception.Message}", "Parking Edge Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (setup is null)
        {
            using SetupForm setupForm = new(client);
            if (setupForm.ShowDialog() != DialogResult.OK) return;
        }

        CentralConnectionResponse? central = null;
        try
        {
            central = client.GetCentralConnectionAsync(CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch
        {
            // 중앙 관리기능만 비활성화하고 기존 로컬 모니터링은 계속 실행한다.
        }

        HttpClient? centralHttpClient = null;
        try
        {
            ICentralParkingClient? centralClient = null;
            if (VehicleManagementFormPolicy.ManagementEnabled(central))
            {
                centralHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(central!.ParkingApiUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };
                centralClient = new CentralParkingClient(
                    centralHttpClient, central.SiteAuthKey);
            }
            Application.Run(new MainForm(
                client, centralClient, centralClient is null ? 0 : central!.SiteId));
        }
        finally
        {
            centralHttpClient?.Dispose();
        }
    }

    private static string LoadBaseUrl()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return "http://localhost:5200/";

        JObject json = JObject.Parse(File.ReadAllText(path));
        string? value = json["EdgeService"]?["BaseUrl"]?.ToString();
        return string.IsNullOrWhiteSpace(value) ? "http://localhost:5200/" : value;
    }
}
