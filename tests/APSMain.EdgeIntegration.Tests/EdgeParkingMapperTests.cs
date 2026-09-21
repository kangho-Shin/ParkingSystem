using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class EdgeParkingMapperTests
{
    [Fact]
    public void Candidate_selection_uses_session_id_when_car_numbers_are_same()
    {
        ParkingSearchCandidate[] candidates =
        {
            new(681, "12가3456", 2, 1, DateTimeOffset.Parse("2026-09-21T03:00:00+09:00"), "OLD.jpg"),
            new(682, "12가3456", 2, 1, DateTimeOffset.Parse("2026-09-21T04:00:00+09:00"), "NEW.jpg")
        };

        ParkingSearchCandidate? selected = EdgeCandidateSelection.Find(candidates, 682);

        Assert.NotNull(selected);
        Assert.Equal(682L, selected.ParkingSessionId);
        Assert.Equal("NEW.jpg", selected.InImage);
    }

    [Fact]
    public void Fee_quote_is_mapped_to_existing_parking_model()
    {
        FeeQuote quote = new()
        {
            ParkingSessionId = 681,
            CarNumber = "서울17가1001",
            EntryAt = DateTimeOffset.Parse("2026-09-21T03:06:49+09:00"),
            ExitAt = DateTimeOffset.Parse("2026-09-21T13:11:08+09:00"),
            PayableAmount = 600,
            Fee = new FeeResult { OriginalFee = 800, DiscountFee = 200, ParkingMinutes = 604 }
        };

        var value = EdgeParkingMapper.FromQuote(quote, 9001, 2, "IN.jpg");

        Assert.Equal(681, value.Xindex);
        Assert.Equal("서울17가1001", value.Carnum);
        Assert.Equal((short)9001, value.Sitenum.GetValueOrDefault());
        Assert.Equal((short)2, value.Groupnum.GetValueOrDefault());
        Assert.Equal((short)3, value.Inhour);
        Assert.Equal((short)6, value.Inmin);
        Assert.Equal("IN.jpg", value.Inimage);
    }
}
