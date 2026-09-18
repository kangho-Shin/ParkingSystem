using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Configuration
{
    public static class SiteConfigurationEndpoints
    {
        public static void MapSiteConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/v1/config/sites/{siteId:long}", async (
                long siteId, ISiteConfigurationRepository repository, CancellationToken cancellationToken) =>
            {
                SiteConfiguration? configuration = await repository.GetAsync(siteId, cancellationToken);
                return configuration is null ? Results.NotFound() : Results.Ok(configuration);
            });

            endpoints.MapPut("/api/v1/config/sites/{siteId:long}", async (
                long siteId, ParkingSite site, ISiteConfigurationRepository repository, CancellationToken cancellationToken) =>
            {
                if (siteId != site.SiteId || siteId <= 0) return Results.BadRequest();
                await repository.SaveSiteAsync(site, cancellationToken);
                return Results.NoContent();
            });

            endpoints.MapPut("/api/v1/config/lanes/{laneId:long}", async (
                long laneId, ParkingLane lane, ISiteConfigurationRepository repository, CancellationToken cancellationToken) =>
            {
                if (laneId != lane.LaneId || laneId <= 0 || lane.SiteId <= 0 || lane.GroupNumber <= 0)
                    return Results.BadRequest();
                await repository.SaveLaneAsync(lane, cancellationToken);
                return Results.NoContent();
            });

            endpoints.MapPut("/api/v1/config/devices/{deviceId:long}", async (
                long deviceId, ParkingDevice device, ISiteConfigurationRepository repository, CancellationToken cancellationToken) =>
            {
                if (deviceId != device.DeviceId || deviceId <= 0 || device.SiteId <= 0 || device.DeviceNumber <= 0)
                    return Results.BadRequest();
                await repository.SaveDeviceAsync(device, cancellationToken);
                return Results.NoContent();
            });
        }
    }
}
