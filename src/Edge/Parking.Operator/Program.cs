using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Parking.Operator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using IHost host = CreateHostBuilder().Build();
        host.Start();
        try
        {
            Application.Run(host.Services.GetRequiredService<MainForm>());
        }
        catch (Exception exception)
        {
            host.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Parking.Operator")
                .LogCritical(exception, "Parking.Operator가 예기치 않게 종료되었습니다.");
            MessageBox.Show(exception.Message, "Parking Operator", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    internal static IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(configuration => configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables())
        .ConfigureServices((context, services) =>
        {
            string baseUrl = context.Configuration["EdgeService:BaseUrl"]
                ?? "http://localhost:5200/";
            services.AddHttpClient<OperatorClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            services.AddTransient<MainForm>();
        });
}
