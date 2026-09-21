namespace Parking.TerminalAgent;

public sealed class TerminalAgentOptions
{
    public int MonitorIntervalSeconds { get; init; } = 10;
    public int MaxRestarts { get; init; } = 3;
    public int RestartWindowMinutes { get; init; } = 5;
    public int StableRunMinutes { get; init; } = 5;
    public string LogDirectory { get; init; } = "logs/TerminalAgent";
    public IReadOnlyList<MonitoredProgram> Programs { get; init; } = Array.Empty<MonitoredProgram>();
}

public sealed class MonitoredProgram
{
    public string Name { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string WorkingDirectory { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public ManagedProgramType ProgramType { get; init; }
}

public enum ManagedProgramType
{
    Unknown,
    Background
}
