using Parking.Contracts;

namespace Parking.Api.Features.Entries
{
    public static class CreateEntryEndpoint
    {
        public static void MapCreateEntryEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/v1/parking/entries", async (
                             FieldEventRequest request,
                             CreateEntryHandler handler,
                             CancellationToken cancellationToken) =>
                         {
                             if (request.EventId == Guid.Empty) {
                                 return Results.BadRequest(new
                                 {
                                     ResultCode = "INVALID_EVENT_ID",
                                     Message = "EventId가 필요합니다."
                                 });
                             }

                             if (request.SiteId <= 0 || request.LaneId <= 0 ||  request.DeviceId <= 0) {
                                 return Results.BadRequest(new
                                 {
                                     ResultCode = "INVALID_DEVICE_IDENTITY",
                                     Message = "현장·차로·장비 번호가 올바르지 않습니다."
                                 });
                             }
                             FieldEventResponse response = await handler.HandleAsync(request, cancellationToken);

                             return Results.Ok(response);
                         });
        }
    }
}