using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class EdgeManagementService
{
    private readonly EdgeMonitoringRepository _monitoring;
    private readonly SqliteOutboxRepository _outbox;
    private readonly LocalConfigurationService _configurationService;
    private readonly GatewayClient _gatewayClient;
    private readonly LprConnectionTracker _lprConnections;

    public EdgeManagementService(
        EdgeMonitoringRepository monitoring,
        SqliteOutboxRepository outbox,
        LocalConfigurationService configurationService,
        GatewayClient gatewayClient,
        LprConnectionTracker lprConnections)
    {
        _monitoring = monitoring;
        _outbox = outbox;
        _configurationService = configurationService;
        _gatewayClient = gatewayClient;
        _lprConnections = lprConnections;
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
            await _configurationService.GetUpdatedAtAsync(cancellationToken);
        int pendingCount = await _outbox.CountPendingAsync(cancellationToken);
        return new EdgeServiceStatus(
            true,
            gatewayHealth.GatewayConnected,
            gatewayHealth.CentralConnected,
            syncedAt,
            pendingCount,
            _lprConnections.ConnectionCount);
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
        _configurationService.GetAsync(cancellationToken);
}
