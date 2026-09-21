using Parking.TerminalAgent;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "Parking Terminal Agent");
builder.Services.Configure<TerminalAgentOptions>(builder.Configuration.GetSection("TerminalAgent"));
TerminalAgentOptions startupOptions = builder.Configuration
    .GetSection("TerminalAgent")
    .Get<TerminalAgentOptions>() ?? new TerminalAgentOptions();
builder.Logging.AddProvider(new DailyFileLoggerProvider(startupOptions.LogDirectory));
builder.Services.AddSingleton<IProcessController, ProcessController>();
builder.Services.AddSingleton(serviceProvider => new ProgramSupervisor(
    serviceProvider.GetRequiredService<IProcessController>(),
    serviceProvider.GetRequiredService<ILogger<ProgramSupervisor>>(),
    startupOptions.MaxRestarts,
    TimeSpan.FromMinutes(startupOptions.RestartWindowMinutes),
    TimeSpan.FromMinutes(startupOptions.StableRunMinutes)));
builder.Services.AddHostedService<ProcessMonitorWorker>();

IHost host = builder.Build();
await host.RunAsync();
