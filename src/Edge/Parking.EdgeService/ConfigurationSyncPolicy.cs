namespace Parking.EdgeService;

public enum ConfigurationSyncAction
{
    None,
    PullRemote,
    PushLocal
}

public static class ConfigurationSyncPolicy
{
    public static ConfigurationSyncAction Resolve(
        bool localDirty,
        long localVersion,
        long? remoteVersion)
    {
        if (localDirty || remoteVersion is null || localVersion > remoteVersion)
            return ConfigurationSyncAction.PushLocal;
        if (remoteVersion > localVersion)
            return ConfigurationSyncAction.PullRemote;
        return ConfigurationSyncAction.None;
    }
}
