using APSMain.BaseClass;
using APSMain.DbModels;

namespace APSMain.Integration.EdgeService;

public static class EdgeSettlementLauncher
{
    public static void Show(Form owner, MainForm main, EdgeServiceClient client,
        KioskExitContext context, bool periodtype = false)
    {
        if (context.Quote is not null)
        {
            ShowQuote(owner, main, client, context, context.Quote, null, periodtype);
            return;
        }
        LoadCandidates(context);
        if (context.Candidates.Count == 0) { MessageBox.Show(owner, "조회된 차량이 없습니다."); return; }
        CarSelectForm selector = new(periodtype);
        selector.EdgeSelectionConfirmed = async parkingSessionId =>
        {
            ParkingSearchCandidate? selected = EdgeCandidateSelection.Find(context.Candidates, parkingSessionId);
            if (selected is null) return;
            EdgeCallResult<FeeQuote> quote = await client.QuoteSessionAsync(
                selected.ParkingSessionId, context.Notification.OutDateTime, Array.Empty<int>());
            if (!quote.IsSuccess || quote.Value is null) { MessageBox.Show(owner, quote.Error ?? "요금 조회에 실패했습니다."); return; }
            context.Quote = quote.Value;
            context.ParkingSessionId = quote.Value.ParkingSessionId;
            ShowQuote(selector, main, client, context, quote.Value, selected.InImage, periodtype);
        };
        UiHost.ShowActiveForm<string>(owner, selector, (_, _) => { });
    }

    public static void Show(Form owner, MainForm15 main, EdgeServiceClient client,
        KioskExitContext context, bool periodtype = false)
    {
        if (context.Quote is not null)
        {
            ShowQuote(owner, main, client, context, context.Quote, null, periodtype);
            return;
        }
        LoadCandidates(context);
        if (context.Candidates.Count == 0) { MessageBox.Show(owner, "조회된 차량이 없습니다."); return; }
        CarSelectForm15 selector = new(periodtype);
        selector.EdgeSelectionConfirmed = async parkingSessionId =>
        {
            ParkingSearchCandidate? selected = EdgeCandidateSelection.Find(context.Candidates, parkingSessionId);
            if (selected is null) return;
            EdgeCallResult<FeeQuote> quote = await client.QuoteSessionAsync(
                selected.ParkingSessionId, context.Notification.OutDateTime, Array.Empty<int>());
            if (!quote.IsSuccess || quote.Value is null) { MessageBox.Show(owner, quote.Error ?? "요금 조회에 실패했습니다."); return; }
            context.Quote = quote.Value;
            context.ParkingSessionId = quote.Value.ParkingSessionId;
            ShowQuote(selector, main, client, context, quote.Value, selected.InImage, periodtype);
        };
        UiHost.ShowActiveForm<string>(owner, selector, (_, _) => { });
    }

    private static void LoadCandidates(KioskExitContext context)
    {
        ParkCache.Parkins.Clear();
        ParkCache.Parkinfos.Clear();
        ParkCache.Periodmembers.Clear();
        foreach (ParkingSearchCandidate candidate in context.Candidates)
            ParkCache.Parkinfos.Add(EdgeParkingMapper.FromCandidate(
                candidate, context.Notification.SiteId));
    }

    private static void ShowQuote(Form owner, MainForm main, EdgeServiceClient client,
        KioskExitContext context, FeeQuote quote, string? inImage, bool periodtype)
    {
        Tparkinfo value = EdgeParkingMapper.FromQuote(
            quote, context.Notification.SiteId, context.Notification.Groupnum, inImage);
        main._xparkinfo.CopyFrom(value);
        ParkCalForm form = new(main._OLDMNum, periodtype) { _parkinfo = main._xparkinfo };
        form.UseEdgeSettlement(client, context, quote);
        UiHost.ShowActiveForm<string>(owner, form, (_, _) => { });
    }

    private static void ShowQuote(Form owner, MainForm15 main, EdgeServiceClient client,
        KioskExitContext context, FeeQuote quote, string? inImage, bool periodtype)
    {
        Tparkinfo value = EdgeParkingMapper.FromQuote(
            quote, context.Notification.SiteId, context.Notification.Groupnum, inImage);
        main._xparkinfo.CopyFrom(value);
        ParkCalForm15 form = new(main._OLDMNum, periodtype) { _parkinfo = main._xparkinfo };
        form.UseEdgeSettlement(client, context, quote);
        UiHost.ShowActiveForm<string>(owner, form, (_, _) => { });
    }
}
