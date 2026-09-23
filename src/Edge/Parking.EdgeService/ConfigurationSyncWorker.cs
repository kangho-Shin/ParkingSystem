using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class ConfigurationSyncWorker : BackgroundService
    {
        private readonly LocalBootstrapStore _bootstrapStore;
        private readonly GatewayClient _gatewayClient;
        private readonly LocalConfigurationStore _store;
        private readonly ILogger<ConfigurationSyncWorker> _logger;

        public ConfigurationSyncWorker(
            GatewayClient gatewayClient,
            LocalBootstrapStore bootstrapStore,
            LocalConfigurationStore store,
            ILogger<ConfigurationSyncWorker> logger)
        {
            _bootstrapStore = bootstrapStore;
            _gatewayClient = gatewayClient;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    EdgeBootstrapSettings? bootstrap = await _bootstrapStore.GetAsync(stoppingToken);
                    if (bootstrap is not null) await SynchronizeAsync(bootstrap, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "현장 설정 동기화 실패");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task SynchronizeAsync(
            EdgeBootstrapSettings bootstrap,
            CancellationToken token)
        {
            VersionedSiteConfiguration? local = await _store.GetVersionedAsync(bootstrap.SiteId, token);
            VersionedSiteConfiguration? remote = await _gatewayClient.GetVersionedConfigurationAsync(
                bootstrap.SiteId, bootstrap.SiteAuthKey, token);
            if (local is null)
            {
                if (remote is not null) await _store.ApplyRemoteAsync(remote, token);
                return;
            }
            bool localDirty = await _store.IsDirtyAsync(bootstrap.SiteId, token);
            ConfigurationSyncAction action = ConfigurationSyncPolicy.Resolve(
                localDirty, local.Version, remote?.Version);
            if (action == ConfigurationSyncAction.PullRemote)
            {
                await _store.ApplyRemoteAsync(remote!, token);
                return;
            }
            if (action == ConfigurationSyncAction.None)
                return;

            VersionedSiteConfiguration outgoing = local with
            {
                Version = Math.Max(local.Version, remote?.Version ?? 0) + 1
            };
            HttpRelayResponse response = await _gatewayClient.SaveVersionedConfigurationAsync(
                outgoing, bootstrap.SiteAuthKey, token);
            if (response.StatusCode is >= 200 and < 300)
                await _store.SetStateAsync(
                    bootstrap.SiteId, outgoing.Version, outgoing.UpdatedAtUtc, token);
            else if (response.StatusCode == StatusCodes.Status409Conflict &&
                     !string.IsNullOrWhiteSpace(response.Content))
            {
                VersionedSiteConfiguration? current =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<VersionedSiteConfiguration>(response.Content);
                if (current is not null && !localDirty)
                    await _store.ApplyRemoteAsync(current, token);
            }
            else
                throw new HttpRequestException($"설정 동기화 실패: HTTP {response.StatusCode}");
        }
    }
}
