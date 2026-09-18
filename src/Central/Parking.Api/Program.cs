using Newtonsoft.Json.Serialization;
using Parking.Api.Features.Entries;
using Parking.Api.Features.Fees;
using Parking.Central.Data;

namespace Parking.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            string connectionString = builder.Configuration.GetConnectionString("ParkingDatabase")
                ?? throw new InvalidOperationException("ParkingDatabase 연결 문자열이 없습니다.");

            builder.Services.AddScoped<IParkingEventRepository>(_ => new ParkingEventRepository(connectionString));
            builder.Services.AddScoped<IParkingExitRepository>(_ => new ParkingExitRepository(connectionString));
            builder.Services.AddScoped<ISiteConfigurationRepository>(_ => new SiteConfigurationRepository(connectionString));
            builder.Services.AddScoped<CreateEntryHandler>();
            builder.Services.AddScoped(_ => new FeeCalculationService(connectionString));

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Parking API");
            app.MapControllers();
            app.Run();
        }
    }
}
