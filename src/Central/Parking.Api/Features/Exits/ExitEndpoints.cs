using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Exits
{
    public static class ExitEndpoints
    {
        public static void MapExitEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/v1/parking/open", async (
                long siteId, string carNumber, IParkingExitRepository repository, CancellationToken cancellationToken) =>
            {
                OpenParkingSessionResponse? session = await repository.FindOpenAsync(siteId, carNumber, cancellationToken);
                return session is null ? Results.NotFound() : Results.Ok(session);
            });

            endpoints.MapPost("/api/v1/parking/exits", async (
                ExitEventRequest request, IParkingExitRepository repository, CancellationToken cancellationToken) =>
            {
                if (request.EventId == Guid.Empty || request.SiteId <= 0 || request.LaneId <= 0 || request.DeviceId <= 0)
                    return Results.BadRequest();
                FieldEventResponse response = await repository.SaveExitAsync(request, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
}
