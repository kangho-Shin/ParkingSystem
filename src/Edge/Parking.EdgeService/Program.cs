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

            string dataDirectory = builder.Configuration["Edge:DataDirectory"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ParkingSystem");
            Directory.CreateDirectory(dataDirectory);

            builder.Services.AddSingleton(new SqliteOutboxRepository($"Data Source={Path.Combine(dataDirectory, "edge.db")}"));
            builder.Services.AddHttpClient<GatewayClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5100/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddHostedService<OutboxWorker>();

            WebApplication app = builder.Build();
            SqliteOutboxRepository outbox = app.Services.GetRequiredService<SqliteOutboxRepository>();
            outbox.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            app.MapGet("/", () => "Parking Edge Service");
            app.MapPost("/api/v1/edge/events", async (FieldEventRequest request, CancellationToken cancellationToken) =>
            {
                await outbox.EnqueueAsync(request, cancellationToken);
                return Results.Accepted(value: new { request.EventId, Queued = true });
            });
            app.Run();
        }
    }
}
