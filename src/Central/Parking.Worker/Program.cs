using Parking.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "Parking Central Worker");
builder.Services.Configure<ParkingApiOptions>(builder.Configuration.GetSection("ParkingApi"));
string baseUrl = builder.Configuration["ParkingApi:BaseUrl"] ?? "http://localhost:5000/";
builder.Services.AddHttpClient<ParkingApiHealthClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHostedService<CentralWorker>();

IHost host = builder.Build();
await host.RunAsync();
