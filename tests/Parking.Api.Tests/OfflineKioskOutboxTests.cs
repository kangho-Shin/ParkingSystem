using Newtonsoft.Json;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class OfflineKioskOutboxTests
{
    [Fact]
    public async Task EnqueueOfflineKioskExitAsync_StoresDedicatedEventTypeAndPayload()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"parking-outbox-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository repository = new($"Data Source={databasePath};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            OfflineKioskExitRequest request = new(
                Guid.NewGuid(), 1, 2, 3, 4, "12가3456",
                DateTimeOffset.Parse("2026-09-19T10:00:00+09:00"), "exit.jpg");

            await repository.EnqueueOfflineKioskExitAsync(request, CancellationToken.None);

            OutboxMessage message = Assert.Single(
                await repository.GetPendingAsync(10, CancellationToken.None));
            Assert.Equal("KioskOfflineExit", message.EventType);
            Assert.Equal(request, JsonConvert.DeserializeObject<OfflineKioskExitRequest>(message.PayloadJson));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
