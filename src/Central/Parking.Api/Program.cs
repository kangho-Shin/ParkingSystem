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
            builder.Services.AddScoped<IParkingSearchRepository>(_ => new ParkingSearchRepository(connectionString));
            builder.Services.AddScoped<IParkingCorrectionRepository>(_ => new ParkingCorrectionRepository(connectionString));
            builder.Services.AddScoped<IParkingLaneDirectionValidator>(_ => new ParkingLaneDirectionValidator(connectionString));
            builder.Services.AddScoped<IPeriodVehicleRepository>(_ => new PeriodVehicleRepository(connectionString));

            builder.Services.AddScoped<IPeriodMemberManagementRepository>(_ => new PeriodMemberManagementRepository(connectionString) );

            builder.Services.AddScoped<IPaymentRepository>(_ => new PaymentRepository(connectionString));
            builder.Services.AddScoped<ISettlementRepository>(_ => new SettlementRepository(connectionString));
            builder.Services.AddScoped<ISiteConfigurationRepository>(_ => new SiteConfigurationRepository(connectionString));
            builder.Services.AddScoped<CreateEntryHandler>();

            builder.Services.AddScoped(_ => new FeeCalculationService(connectionString));
            builder.Services.AddScoped<ParkingQuoteService>();

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Parking API");
            app.MapControllers();
            app.Run();
        }
    }
}
