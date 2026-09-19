using Newtonsoft.Json.Linq;
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
        Application.Run(new MainForm(client));
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
