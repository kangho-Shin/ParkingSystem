using Newtonsoft.Json.Serialization;

namespace Parking.EdgeService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Host.UseWindowsService(options => options.ServiceName = "Parking Edge Service");
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new DefaultContractResolver());
            builder.Services.AddSignalR();

            string? configuredDataDirectory = builder.Configuration["Edge:DataDirectory"];
            string dataDirectory = EdgeDataDirectory.Resolve(
                configuredDataDirectory,
                AppContext.BaseDirectory);
            Directory.CreateDirectory(dataDirectory);
            string sqliteConnectionString = $"Data Source={Path.Combine(dataDirectory, "edge.db")}";

            builder.Services.AddSingleton(new SqliteOutboxRepository(sqliteConnectionString));
            builder.Services.AddSingleton(new LocalBootstrapStore(sqliteConnectionString));
            builder.Services.AddSingleton(new LocalConfigurationStore(sqliteConnectionString));
            builder.Services.AddSingleton(new EdgeMonitoringRepository(sqliteConnectionString));
            builder.Services.AddSingleton(new KioskPendingRepository(sqliteConnectionString));
            builder.Services.AddHttpClient<GatewayClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5100/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddScoped<EdgeEventService>();
            builder.Services.AddScoped<PaymentRelayService>();
            builder.Services.AddScoped<EdgeManagementService>();
            builder.Services.AddScoped<LocalConfigurationService>();
            builder.Services.AddScoped<EdgeDeviceResolver>();
            builder.Services.AddScoped<IEdgeDeviceResolver>(services =>
                services.GetRequiredService<EdgeDeviceResolver>());
            builder.Services.AddScoped<KioskExitCoordinator>();
            builder.Services.AddScoped<IKioskExitCoordinator>(services =>
                services.GetRequiredService<KioskExitCoordinator>());
            builder.Services.AddSingleton<LprFileNameParser>();
            builder.Services.AddSingleton<LprConnectionTracker>();
            builder.Services.AddSingleton<KioskConnectionRegistry>();
            builder.Services.AddSingleton<IKioskNotifier, SignalRKioskNotifier>();
            builder.Services.AddSingleton<KioskNotificationService>();
            builder.Services.AddSingleton<IDisplayBoardOutput, DisplayBoardOutput>();
            builder.Services.AddScoped<LprLaneProcessor>();
            builder.Services.AddHostedService<OutboxWorker>();
            builder.Services.AddHostedService<ConfigurationSyncWorker>();
            builder.Services.AddHostedService<DisplayBoardClockWorker>();
            builder.Services.AddHostedService<LprTcpWorker>();

            WebApplication app = builder.Build();
            app.Services.GetRequiredService<SqliteOutboxRepository>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<LocalBootstrapStore>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<LocalConfigurationStore>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<EdgeMonitoringRepository>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<KioskPendingRepository>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            app.MapGet("/", () => "Parking Edge Service");
            app.MapControllers();
            app.MapHub<KioskHub>("/hubs/kiosk");
            app.Run();
        }
    }
}
