using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public interface IEdgeManagerView
{
    void ShowStatus(EdgeServiceStatus status);
    void ShowDisconnected();
    void ShowEntries(IReadOnlyList<EdgeEntryItem> entries);
    void ShowActivities(IReadOnlyList<EdgeActivityItem> activities);
    void ShowConfiguration(SiteConfiguration? configuration);
    void ShowImages(byte[]? inImage, byte[]? outImage);
}
