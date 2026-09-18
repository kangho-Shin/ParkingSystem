using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class ConfigurationSyncWorker : BackgroundService
    {
        private readonly long _siteId;
        private readonly GatewayClient _gatewayClient;
        private readonly LocalConfigurationStore _store;
        private readonly ILogger<ConfigurationSyncWorker> _logger;

        public ConfigurationSyncWorker(
            IConfiguration configuration,
            GatewayClient gatewayClient,
            LocalConfigurationStore store,
            ILogger<ConfigurationSyncWorker> logger)
        {
            _siteId = configuration.GetValue<long>("Edge:SiteId");
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
                    if (_siteId > 0)
                    {
                        SiteConfiguration configuration =
                            await _gatewayClient.GetSiteConfigurationAsync(_siteId, stoppingToken);
                        await _store.SaveAsync(configuration, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "현장 설정 동기화 실패: SiteId={SiteId}", _siteId);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
