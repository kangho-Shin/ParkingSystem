using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class EdgeManagementService
{
    private readonly long _siteId;
    private readonly EdgeMonitoringRepository _monitoring;
    private readonly SqliteOutboxRepository _outbox;
    private readonly LocalConfigurationStore _configurationStore;
    private readonly GatewayClient _gatewayClient;

    public EdgeManagementService(
        IConfiguration configuration,
        EdgeMonitoringRepository monitoring,
        SqliteOutboxRepository outbox,
        LocalConfigurationStore configurationStore,
        GatewayClient gatewayClient)
    {
        _siteId = configuration.GetValue<long>("Edge:SiteId");
        _monitoring = monitoring;
        _outbox = outbox;
        _configurationStore = configurationStore;
        _gatewayClient = gatewayClient;
    }

    public async Task<EdgeServiceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        GatewayHealthResponse gatewayHealth;
        try
        {
            gatewayHealth = await _gatewayClient.GetHealthAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is TaskCanceledException)
        {
            gatewayHealth = new GatewayHealthResponse(false, false);
        }

        DateTimeOffset? syncedAt =
            await _configurationStore.GetSyncedAtAsync(cancellationToken);
        int pendingCount = await _outbox.CountPendingAsync(cancellationToken);
        return new EdgeServiceStatus(
            true,
            gatewayHealth.GatewayConnected,
            gatewayHealth.CentralConnected,
            syncedAt,
            pendingCount);
    }

    public Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(
        int limit,
        CancellationToken cancellationToken) =>
        _monitoring.GetEntriesAsync(limit, cancellationToken);

    public Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(
        int limit,
        CancellationToken cancellationToken) =>
        _monitoring.GetActivitiesAsync(limit, cancellationToken);

    public Task<SiteConfiguration?> GetConfigurationAsync(
        CancellationToken cancellationToken) =>
        _configurationStore.GetAsync(_siteId, cancellationToken);
}
