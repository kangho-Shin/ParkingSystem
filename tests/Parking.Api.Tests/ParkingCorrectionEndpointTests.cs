using Microsoft.AspNetCore.Mvc;
using Parking.Api.Features.Search;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class ParkingCorrectionEndpointTests
{
    [Fact]
    public async Task 미출차_차량번호를_수정한다()
    {
        FakeRepository repository = new();
        ParkingCorrectionController controller = new(repository);

        IActionResult action = await controller.CorrectAsync(
            123,
            new CorrectCarNumberRequest("12가3456"),
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        CorrectCarNumberResponse result = Assert.IsType<CorrectCarNumberResponse>(ok.Value);
        Assert.True(result.Updated);
        Assert.Equal(123, repository.ParkingSessionId);
        Assert.Equal("12가3456", repository.CarNumber);
    }

    [Fact]
    public async Task 빈_차량번호는_거부한다()
    {
        ParkingCorrectionController controller = new(new FakeRepository());
        IActionResult action = await controller.CorrectAsync(
            123,
            new CorrectCarNumberRequest(" "),
            CancellationToken.None);
        Assert.IsType<BadRequestResult>(action);
    }

    private sealed class FakeRepository : IParkingCorrectionRepository
    {
        public long ParkingSessionId { get; private set; }
        public string CarNumber { get; private set; } = "";

        public Task<CorrectCarNumberResponse> CorrectCarNumberAsync(long parkingSessionId, string carNumber, CancellationToken cancellationToken)
        {
            ParkingSessionId = parkingSessionId;
            CarNumber = carNumber;
            return Task.FromResult(new CorrectCarNumberResponse(parkingSessionId, carNumber, true, "CAR_NUMBER_UPDATED", "수정 완료"));
        }
    }
}
