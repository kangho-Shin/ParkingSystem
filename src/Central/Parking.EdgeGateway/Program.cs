using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);
            string apiBaseUrl = builder.Configuration["ParkingApi:BaseUrl"] ?? "http://localhost:5000/";

            builder.Services.AddHttpClient<ParkingApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddScoped<FieldEventRelay>();

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Parking Edge Gateway");

            app.MapPost("/api/v1/edge/events", async (
                FieldEventRequest request, FieldEventRelay relay, CancellationToken cancellationToken) =>
                await RelayAsync(() => relay.RelayAsync(request, cancellationToken)));

            app.MapPost("/api/v1/edge/exits", async (
                ExitEventRequest request, FieldEventRelay relay, CancellationToken cancellationToken) =>
                await RelayAsync(() => relay.RelayExitAsync(request, cancellationToken)));

            app.Run();
        }

        private static async Task<IResult> RelayAsync(Func<Task<FieldEventResponse>> action)
        {
            try { return Results.Ok(await action()); }
            catch (HttpRequestException) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
            catch (TaskCanceledException) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
        }
    }
}
