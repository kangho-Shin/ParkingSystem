using Microsoft.Extensions.Logging.Abstractions;
using Parking.TerminalAgent;

namespace Parking.TerminalAgent.Tests;

public sealed class ProgramSupervisorTests
{
    [Fact]
    public void StartsEnabledProgramWhenItIsNotRunning()
    {
        FakeProcessController processes = new();
        ProgramSupervisor supervisor = CreateSupervisor(processes);
        MonitoredProgram program = CreateProgram();

        ProgramCheckResult result = supervisor.Check(program, Utc(0));

        Assert.Equal(ProgramCheckStatus.Started, result.Status);
        Assert.Single(processes.StartedPrograms);
        Assert.Equal(@"C:\Parking\Parking.KioskSimulator.exe", processes.StartedPrograms[0].ExecutablePath);
    }

    [Fact]
    public void DoesNotStartProgramThatIsAlreadyRunning()
    {
        FakeProcessController processes = new() { IsRunning = true };
        ProgramSupervisor supervisor = CreateSupervisor(processes);

        ProgramCheckResult result = supervisor.Check(CreateProgram(), Utc(0));

        Assert.Equal(ProgramCheckStatus.Running, result.Status);
        Assert.Empty(processes.StartedPrograms);
    }

    [Fact]
    public void StopsRestartingAfterRepeatedFailures()
    {
        FakeProcessController processes = new();
        ProgramSupervisor supervisor = CreateSupervisor(processes, maxRestarts: 2);
        MonitoredProgram program = CreateProgram();

        Assert.Equal(ProgramCheckStatus.Started, supervisor.Check(program, Utc(0)).Status);
        Assert.Equal(ProgramCheckStatus.Started, supervisor.Check(program, Utc(1)).Status);
        ProgramCheckResult blocked = supervisor.Check(program, Utc(2));

        Assert.Equal(ProgramCheckStatus.RestartLimited, blocked.Status);
        Assert.Equal(Utc(5), blocked.RetryAfter);
        Assert.Equal(2, processes.StartedPrograms.Count);
    }

    [Fact]
    public void StableRunningProgramResetsRestartLimit()
    {
        FakeProcessController processes = new();
        ProgramSupervisor supervisor = CreateSupervisor(processes, maxRestarts: 2, stableMinutes: 5);
        MonitoredProgram program = CreateProgram();

        supervisor.Check(program, Utc(0));
        processes.IsRunning = true;
        supervisor.Check(program, Utc(5));
        processes.IsRunning = false;

        Assert.Equal(ProgramCheckStatus.Started, supervisor.Check(program, Utc(6)).Status);
    }

    [Fact]
    public void RejectsProgramNotExplicitlyMarkedAsBackground()
    {
        FakeProcessController processes = new();
        ProgramSupervisor supervisor = CreateSupervisor(processes);
        MonitoredProgram program = new()
        {
            Name = "EdgeManager",
            ProcessName = "Parking.EdgeManager",
            ExecutablePath = @"C:\Parking\Parking.EdgeManager.exe",
            Enabled = true,
            ProgramType = ManagedProgramType.Unknown
        };

        ProgramCheckResult result = supervisor.Check(program, Utc(0));

        Assert.Equal(ProgramCheckStatus.InvalidConfiguration, result.Status);
        Assert.Empty(processes.StartedPrograms);
    }

    private static ProgramSupervisor CreateSupervisor(
        FakeProcessController processes,
        int maxRestarts = 3,
        int stableMinutes = 5) =>
        new(processes, NullLogger<ProgramSupervisor>.Instance, maxRestarts,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(stableMinutes));

    private static MonitoredProgram CreateProgram() => new()
    {
        Name = "KioskSimulator",
        ProcessName = "Parking.KioskSimulator",
        ExecutablePath = @"C:\Parking\Parking.KioskSimulator.exe",
        Arguments = "--site 9001 --group 2 --device-number 201 --complete",
        WorkingDirectory = @"C:\Parking",
        Enabled = true,
        ProgramType = ManagedProgramType.Background
    };

    private static DateTimeOffset Utc(int minute) =>
        new(2026, 9, 20, 12, minute, 0, TimeSpan.Zero);

    private sealed class FakeProcessController : IProcessController
    {
        public bool IsRunning { get; set; }
        public List<MonitoredProgram> StartedPrograms { get; } = [];

        public bool IsProcessRunning(string processName) => IsRunning;

        public void Start(MonitoredProgram program) => StartedPrograms.Add(program);
    }
}
