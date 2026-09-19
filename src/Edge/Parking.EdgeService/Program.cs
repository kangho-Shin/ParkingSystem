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

            string? configuredDataDirectory = builder.Configuration["Edge:DataDirectory"];
            string dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
                ? (Environment.UserInteractive
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParkingSystem")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ParkingSystem"))
                : configuredDataDirectory;
            Directory.CreateDirectory(dataDirectory);
            string sqliteConnectionString = $"Data Source={Path.Combine(dataDirectory, "edge.db")}";

            builder.Services.AddSingleton(new SqliteOutboxRepository(sqliteConnectionString));
            builder.Services.AddSingleton(new LocalConfigurationStore(sqliteConnectionString));
            builder.Services.AddSingleton(new EdgeMonitoringRepository(sqliteConnectionString));
            builder.Services.AddHttpClient<GatewayClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5100/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddScoped<EdgeEventService>();
            builder.Services.AddScoped<PaymentRelayService>();
            builder.Services.AddHostedService<OutboxWorker>();
            builder.Services.AddHostedService<ConfigurationSyncWorker>();

            WebApplication app = builder.Build();
            app.Services.GetRequiredService<SqliteOutboxRepository>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<LocalConfigurationStore>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.Services.GetRequiredService<EdgeMonitoringRepository>()
                .InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            app.MapGet("/", () => "Parking Edge Service");
            app.MapControllers();
            app.Run();
        }
    }
}
