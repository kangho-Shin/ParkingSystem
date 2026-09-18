using Parking.Api.Features.Configuration;
using Parking.Api.Features.Entries;
using Parking.Api.Features.Exits;
using Parking.Central.Data;

namespace Parking.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);

            string connectionString = builder.Configuration.GetConnectionString("ParkingDatabase")
                ?? throw new InvalidOperationException("ParkingDatabase 연결 문자열이 없습니다.");

            builder.Services.AddScoped<IParkingEventRepository>(_ => new ParkingEventRepository(connectionString));
            builder.Services.AddScoped<IParkingExitRepository>(_ => new ParkingExitRepository(connectionString));
            builder.Services.AddScoped<ISiteConfigurationRepository>(_ => new SiteConfigurationRepository(connectionString));
            builder.Services.AddScoped<CreateEntryHandler>();

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Parking API");
            app.MapCreateEntryEndpoint();
            app.MapExitEndpoints();
            app.MapSiteConfigurationEndpoints();
            app.Run();
        }
    }
}
