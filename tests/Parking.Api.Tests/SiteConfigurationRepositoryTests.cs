using Parking.Central.Data;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class SiteConfigurationRepositoryTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 버전설정의_MySql일시를_UTC로_조회한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        var configuration = await repository.GetVersionedAsync(9001, CancellationToken.None);

        Assert.NotNull(configuration);
        Assert.Equal(TimeSpan.Zero, configuration.UpdatedAtUtc.Offset);
    }
}
