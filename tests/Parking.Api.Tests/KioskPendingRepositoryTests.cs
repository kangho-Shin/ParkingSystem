using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class KioskPendingRepositoryTests
{
    [Fact]
    public async Task 같은EventId는_한번만저장하고_완료후대기목록에서제외한다()
    {
        string path = Path.Combine(Path.GetTempPath(), $"kiosk-pending-{Guid.NewGuid():N}.db");
        try
        {
            KioskPendingRepository repository = new($"Data Source={path};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            Guid eventId = Guid.NewGuid();
            LprRecognition recognition = new(
                eventId, 1, 1, 101, 10, "Exit", DateTimeOffset.Now,
                "12가3456", $"001_001_101_010_Exit_20260919153025123_12가3456_{eventId:N}.jpg");

            await repository.EnqueueAsync(301, recognition, CancellationToken.None);
            await repository.EnqueueAsync(301, recognition, CancellationToken.None);
            Assert.Single(await repository.GetPendingAsync(301, CancellationToken.None));

            await repository.MarkCompletedAsync(eventId, CancellationToken.None);
            Assert.Empty(await repository.GetPendingAsync(301, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task 완료처리는_같은EventId를_한번만선점한다()
    {
        string path = Path.Combine(Path.GetTempPath(), $"kiosk-claim-{Guid.NewGuid():N}.db");
        try
        {
            KioskPendingRepository repository = new($"Data Source={path};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            Guid eventId = Guid.NewGuid();
            LprRecognition recognition = new(
                eventId, 1, 1, 101, 10, "Exit", DateTimeOffset.Now,
                "12가3456", $"001_001_101_010_Exit_20260919153025123_12가3456_{eventId:N}.jpg");
            await repository.EnqueueAsync(301, recognition, CancellationToken.None);

            Assert.True(await repository.TryBeginCompletionAsync(eventId, CancellationToken.None));
            Assert.False(await repository.TryBeginCompletionAsync(eventId, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task 장애출차선점후에는_재연결대기목록에서제외한다()
    {
        string path = Path.Combine(Path.GetTempPath(), $"kiosk-offline-claim-{Guid.NewGuid():N}.db");
        try
        {
            KioskPendingRepository repository = new($"Data Source={path};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            Guid eventId = Guid.NewGuid();
            LprRecognition recognition = new(
                eventId, 1, 1, 101, 10, "Exit", DateTimeOffset.Now,
                "12가3456", $"001_001_101_010_Exit_20260919153025123_12가3456_{eventId:N}.jpg");
            await repository.EnqueueAsync(301, recognition, CancellationToken.None);

            Assert.True(await repository.TryBeginOfflineOpenAsync(eventId, CancellationToken.None));
            Assert.Empty(await repository.GetPendingAsync(301, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
