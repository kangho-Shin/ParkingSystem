using Newtonsoft.Json.Serialization;

namespace Parking.EdgeGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            string apiBaseUrl = builder.Configuration["ParkingApi:BaseUrl"] ?? "http://localhost:5000/";
            builder.Services.AddHttpClient<ParkingApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddScoped<FieldEventRelay>();

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Parking Edge Gateway");
            app.MapControllers();
            app.Run();
        }
    }
}
