using System.Text.RegularExpressions;

namespace Parking.Api.Tests;

public sealed class DatabaseScriptSafetyTests
{
    [Fact]
    public void MySql_migration_scripts_preserve_existing_tables()
    {
        string root = FindRepositoryRoot();
        foreach (string path in Directory.GetFiles(
                     Path.Combine(root, "database", "mysql"), "*.sql")
                 .Where(path => !string.Equals(
                     Path.GetFileName(path),
                     "newfull_schema.sql",
                     StringComparison.OrdinalIgnoreCase)))
        {
            string sql = File.ReadAllText(path);
            Assert.DoesNotMatch(new Regex(@"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase), sql);
            Assert.DoesNotMatch(new Regex(@"\bCREATE\s+TABLE\s+(?!IF\s+NOT\s+EXISTS\b)", RegexOptions.IgnoreCase), sql);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("ParkingSystem.sln");
    }
}
