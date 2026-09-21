namespace Parking.TerminalAgent;

public enum ProgramCheckStatus
{
    Disabled,
    InvalidConfiguration,
    Running,
    Started,
    RestartLimited,
    StartFailed
}

public sealed record ProgramCheckResult(
    ProgramCheckStatus Status,
    DateTimeOffset? RetryAfter = null,
    string? ErrorMessage = null);

public sealed class ProgramSupervisor
{
    private readonly IProcessController _processes;
    private readonly ILogger<ProgramSupervisor> _logger;
    private readonly int _maxRestarts;
    private readonly TimeSpan _restartWindow;
    private readonly TimeSpan _stableRun;
    private readonly Dictionary<string, ProgramState> _states = new(StringComparer.OrdinalIgnoreCase);

    public ProgramSupervisor(
        IProcessController processes,
        ILogger<ProgramSupervisor> logger,
        int maxRestarts,
        TimeSpan restartWindow,
        TimeSpan stableRun)
    {
        _processes = processes;
        _logger = logger;
        _maxRestarts = Math.Max(maxRestarts, 1);
        _restartWindow = restartWindow > TimeSpan.Zero ? restartWindow : TimeSpan.FromMinutes(5);
        _stableRun = stableRun > TimeSpan.Zero ? stableRun : TimeSpan.FromMinutes(5);
    }

    public ProgramCheckResult Check(MonitoredProgram program, DateTimeOffset now)
    {
        if (!program.Enabled)
            return new(ProgramCheckStatus.Disabled);

        if (program.ProgramType != ManagedProgramType.Background)
        {
            string message = $"{program.Name}은 Background 프로그램으로 명시되지 않아 실행하지 않습니다.";
            _logger.LogError("감시 프로그램 설정 오류: {Message}", message);
            return new(ProgramCheckStatus.InvalidConfiguration, ErrorMessage: message);
        }

        if (string.IsNullOrWhiteSpace(program.Name) || string.IsNullOrWhiteSpace(program.ProcessName))
        {
            const string message = "Name 또는 ProcessName 설정이 비어 있습니다.";
            _logger.LogError("감시 프로그램 설정 오류: {Message}", message);
            return new(ProgramCheckStatus.InvalidConfiguration, ErrorMessage: message);
        }

        ProgramState state = GetState(program.Name);
        if (_processes.IsProcessRunning(program.ProcessName))
        {
            if (state.StartedAt.HasValue && now - state.StartedAt.Value >= _stableRun)
            {
                state.RestartLimiter.Reset();
                state.StartedAt = null;
                state.LimitLogged = false;
                _logger.LogInformation("프로그램 {ProgramName}이 안정 실행되어 재시작 횟수를 초기화했습니다.", program.Name);
            }
            return new(ProgramCheckStatus.Running);
        }

        if (!state.RestartLimiter.TryAcquire(now, out DateTimeOffset retryAfter))
        {
            if (!state.LimitLogged)
            {
                _logger.LogError(
                    "프로그램 {ProgramName}이 반복 종료되어 {RetryAfter}까지 재시작을 중지합니다.",
                    program.Name, retryAfter.LocalDateTime);
                state.LimitLogged = true;
            }
            return new(ProgramCheckStatus.RestartLimited, retryAfter);
        }

        state.LimitLogged = false;
        try
        {
            _processes.Start(program);
            state.StartedAt = now;
            _logger.LogWarning("중지된 프로그램 {ProgramName}을 실행했습니다. 경로: {ExecutablePath}",
                program.Name, program.ExecutablePath);
            return new(ProgramCheckStatus.Started);
        }
        catch (Exception exception)
        {
            state.StartedAt = null;
            _logger.LogError(exception, "프로그램 {ProgramName} 실행에 실패했습니다.", program.Name);
            return new(ProgramCheckStatus.StartFailed, ErrorMessage: exception.Message);
        }
    }

    private ProgramState GetState(string name)
    {
        if (_states.TryGetValue(name, out ProgramState? state))
            return state;

        state = new ProgramState(new RestartLimiter(_maxRestarts, _restartWindow));
        _states.Add(name, state);
        return state;
    }

    private sealed class ProgramState(RestartLimiter restartLimiter)
    {
        public RestartLimiter RestartLimiter { get; } = restartLimiter;
        public DateTimeOffset? StartedAt { get; set; }
        public bool LimitLogged { get; set; }
    }
}
