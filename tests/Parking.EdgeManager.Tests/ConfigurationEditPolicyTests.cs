using System.Data;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class ConfigurationEditPolicyTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void 변경중인_체크박스만_즉시_확정한다(
        bool isDirty,
        bool isCheckBox,
        bool expected)
    {
        Assert.Equal(expected, ConfigurationEditPolicy.ShouldCommit(isDirty, isCheckBox));
    }

    [Theory]
    [InlineData(DataRowState.Added, true)]
    [InlineData(DataRowState.Modified, true)]
    [InlineData(DataRowState.Unchanged, false)]
    [InlineData(DataRowState.Deleted, false)]
    public void 추가되거나_수정된_행만_저장한다(DataRowState state, bool expected)
    {
        Assert.Equal(expected, ConfigurationEditPolicy.ShouldSave(state));
    }
}
