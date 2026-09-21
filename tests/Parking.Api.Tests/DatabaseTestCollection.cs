using MySqlConnector;

namespace Parking.Api.Tests;

[CollectionDefinition("Database", DisableParallelization = true)]
public sealed class DatabaseTestCollection : ICollectionFixture<DatabaseSafetyFixture>
{
}

public sealed class DatabaseSafetyFixture
{
    public DatabaseSafetyFixture()
    {
        string value = Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
            ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");
        string database = new MySqlConnectionStringBuilder(value).Database;
        if (!string.Equals(database, "parking000test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"DB 테스트는 parking000test에서만 실행할 수 있습니다. 현재 DB: {database}");
    }
}
