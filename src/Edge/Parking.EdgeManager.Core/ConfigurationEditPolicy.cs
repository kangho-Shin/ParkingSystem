namespace Parking.EdgeManager.Core;

public static class ConfigurationEditPolicy
{
    public static bool ShouldCommit(bool isDirty, bool isCheckBox) =>
        isDirty && isCheckBox;
}
