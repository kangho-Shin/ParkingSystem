using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class LocalConfigurationService
{
    private readonly LocalBootstrapStore _bootstrap;
    private readonly LocalConfigurationStore _store;
    public LocalConfigurationService(LocalBootstrapStore bootstrap, LocalConfigurationStore store) { _bootstrap = bootstrap; _store = store; }

    public async Task<long> GetSiteIdAsync(CancellationToken token) =>
        (await _bootstrap.GetAsync(token))?.SiteId ?? throw new InvalidOperationException("현장 최초 설정이 필요합니다.");
    public async Task<SiteConfiguration?> GetAsync(CancellationToken token) => await _store.GetAsync(await GetSiteIdAsync(token), token);
    public Task<DateTimeOffset?> GetUpdatedAtAsync(CancellationToken token) => _store.GetSyncedAtAsync(token);
    public async Task SaveSiteAsync(ParkingSite x, CancellationToken token) { await CheckAsync(x.SiteId, token); await _store.SaveSiteAsync(x, token); }
    public async Task SaveLaneAsync(ParkingLane x, CancellationToken token) { await CheckAsync(x.SiteId, token); await _store.SaveLaneAsync(x, token); }
    public async Task SaveDeviceAsync(ParkingDevice x, CancellationToken token) { await CheckAsync(x.SiteId, token); await _store.SaveDeviceAsync(x, token); }
    public async Task SaveDeviceLinkAsync(ParkingDeviceLink x, CancellationToken token) { await CheckAsync(x.SiteId, token); await _store.SaveDeviceLinkAsync(x, token); }
    public async Task DeleteLaneAsync(long id, CancellationToken token) => await _store.DeleteLaneAsync(await GetSiteIdAsync(token), id, token);
    public async Task DeleteDeviceAsync(long id, CancellationToken token) => await _store.DeleteDeviceAsync(await GetSiteIdAsync(token), id, token);
    public async Task DeleteDeviceLinkAsync(long sourceId, long targetId, string type, CancellationToken token) => await _store.DeleteDeviceLinkAsync(await GetSiteIdAsync(token), sourceId, targetId, type, token);
    public async Task SaveOperationVariableAsync(ParkingOperationVariable x, CancellationToken token) => await _store.SaveOperationVariableAsync(await GetSiteIdAsync(token), x, token);
    public async Task DeleteOperationVariableAsync(int groupnum, string commandType, CancellationToken token) => await _store.DeleteOperationVariableAsync(await GetSiteIdAsync(token), groupnum, commandType, token);
    private async Task CheckAsync(long siteId, CancellationToken token) { long local = await GetSiteIdAsync(token); if (siteId != local) throw new InvalidOperationException($"다른 현장 설정은 저장할 수 없습니다. Local={local}, Requested={siteId}"); }
}
