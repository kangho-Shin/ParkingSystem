namespace Parking.EdgeService;

public static class EdgeDataDirectory
{
    public static string Resolve(string? configuredDirectory, string baseDirectory) =>
        string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(baseDirectory, "Data")
            : configuredDirectory;
}
