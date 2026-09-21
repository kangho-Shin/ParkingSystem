using System.Diagnostics;

namespace Parking.TerminalAgent;

public interface IProcessController
{
    bool IsProcessRunning(string processName);
    void Start(MonitoredProgram program);
}

public sealed class ProcessController : IProcessController
{
    public bool IsProcessRunning(string processName)
    {
        string normalizedName = Path.GetFileNameWithoutExtension(processName);
        Process[] processes = Process.GetProcessesByName(normalizedName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (Process process in processes)
                process.Dispose();
        }
    }

    public void Start(MonitoredProgram program)
    {
        if (string.IsNullOrWhiteSpace(program.ExecutablePath))
            throw new InvalidOperationException($"{program.Name}의 ExecutablePath 설정이 비어 있습니다.");
        if (!File.Exists(program.ExecutablePath))
            throw new FileNotFoundException($"{program.Name} 실행 파일을 찾을 수 없습니다.", program.ExecutablePath);

        string workingDirectory = string.IsNullOrWhiteSpace(program.WorkingDirectory)
            ? Path.GetDirectoryName(program.ExecutablePath) ?? AppContext.BaseDirectory
            : program.WorkingDirectory;

        ProcessStartInfo startInfo = new()
        {
            FileName = program.ExecutablePath,
            Arguments = program.Arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException($"{program.Name} 프로세스를 시작하지 못했습니다.");
        process.Dispose();
    }
}
