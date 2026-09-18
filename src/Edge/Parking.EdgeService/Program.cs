using Parking.Contracts;

namespace Parking.EdgeService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Host.UseWindowsService(options => options.ServiceName = "Parking Edge Service");
            builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);

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
            builder.Services.AddHttpClient<GatewayClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5100/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddScoped<EdgeEventService>();
            builder.Services.AddHostedService<OutboxWorker>();
            builder.Services.AddHostedService<ConfigurationSyncWorker>();

            WebApplication app = builder.Build();
            SqliteOutboxRepository outbox = app.Services.GetRequiredService<SqliteOutboxRepository>();
            LocalConfigurationStore configurationStore = app.Services.GetRequiredService<LocalConfigurationStore>();
            outbox.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            configurationStore.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            app.MapGet("/", () => "Parking Edge Service");
            app.MapGet("/api/v1/local/config", async (IConfiguration configuration, CancellationToken cancellationToken) =>
            {
                long siteId = configuration.GetValue<long>("Edge:SiteId");
                SiteConfiguration? result = await configurationStore.GetAsync(siteId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });
            app.MapPost("/api/v1/edge/events", async (
                FieldEventRequest request, EdgeEventService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.AcceptEntryAsync(request, cancellationToken)));
            app.MapPost("/api/v1/edge/exits", async (
                ExitEventRequest request, EdgeEventService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.AcceptExitAsync(request, cancellationToken)));
            app.Run();
        }
    }
}
