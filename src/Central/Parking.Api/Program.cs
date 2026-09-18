using Parking.Api.Features.Entries;
using Parking.Central.Data;

namespace Parking.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder =
                WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<IParkingEventRepository>(_ =>
                new ParkingEventRepository(
                    builder.Configuration.GetConnectionString("ParkingDatabase")
                    ?? throw new InvalidOperationException(
                        "ParkingDatabase 연결 문자열이 없습니다.")));

            builder.Services.AddScoped<CreateEntryHandler>();

            WebApplication app = builder.Build();

            app.MapGet("/", () => "Parking API");
            app.MapCreateEntryEndpoint();

            app.Run();
        }
    }
}