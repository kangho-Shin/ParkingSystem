using Microsoft.Extensions.Logging;
using Parking.TerminalAgent;

namespace Parking.TerminalAgent.Tests;

public sealed class DailyFileLoggerProviderTests
{
    [Fact]
    public void WritesLogIntoDateDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"terminal-agent-{Guid.NewGuid():N}");
        try
        {
            DateTime before = DateTime.Now;
            using DailyFileLoggerProvider provider = new(root);
            ILogger logger = provider.CreateLogger("TerminalAgentTest");

            logger.LogInformation("프로그램 시작 시험");

            DateTime after = DateTime.Now;
            string beforePath = LogPath(root, before);
            string afterPath = LogPath(root, after);
            string path = File.Exists(beforePath) ? beforePath : afterPath;
            Assert.Contains("프로그램 시작 시험", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static string LogPath(string root, DateTime date) =>
        Path.Combine(root, date.ToString("yyyy"), date.ToString("MM"),
            date.ToString("dd"), "TerminalAgent.log");
}
