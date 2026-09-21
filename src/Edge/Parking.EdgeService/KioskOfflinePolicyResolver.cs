using Parking.Contracts;

namespace Parking.EdgeService;

public enum KioskOfflinePolicy { Open, Block }

public static class KioskOfflinePolicyResolver
{
    public static KioskOfflinePolicy Resolve(SiteConfiguration configuration, int groupnum)
    {
        string? value = configuration.OperationVariables?
            .FirstOrDefault(variable => variable.Groupnum == groupnum &&
                string.Equals(variable.CommandType, "CMD_KIOSK_OFFLINE_POLICY", StringComparison.OrdinalIgnoreCase))?
            .Value;
        return string.Equals(value, "BLOCK", StringComparison.OrdinalIgnoreCase)
            ? KioskOfflinePolicy.Block
            : KioskOfflinePolicy.Open;
    }
}
