using System.Data;

namespace Parking.EdgeManager.Core;

public static class ConfigurationEditPolicy
{
    public static bool ShouldCommit(bool isDirty, bool isCheckBox) =>
        isDirty && isCheckBox;

    public static bool ShouldSave(DataRowState state) =>
        state is DataRowState.Added or DataRowState.Modified;
}
