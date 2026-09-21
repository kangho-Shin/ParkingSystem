using Microsoft.Extensions.Options;

namespace Parking.TerminalAgent;

public sealed class ProcessMonitorWorker : BackgroundService
{
    private readonly ILogger<ProcessMonitorWorker> _logger;
    private readonly IOptions<TerminalAgentOptions> _options;
    private readonly ProgramSupervisor _supervisor;

    public ProcessMonitorWorker(
        ILogger<ProcessMonitorWorker> logger,
        IOptions<TerminalAgentOptions> options,
        ProgramSupervisor supervisor)
    {
        _logger = logger;
        _options = options;
        _supervisor = supervisor;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parking.TerminalAgent가 시작되었습니다.");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parking.TerminalAgent가 종료됩니다.");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TerminalAgentOptions options = _options.Value;
            foreach (MonitoredProgram program in options.Programs)
                _supervisor.Check(program, DateTimeOffset.Now);

            int seconds = Math.Max(options.MonitorIntervalSeconds, 1);
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }
}
