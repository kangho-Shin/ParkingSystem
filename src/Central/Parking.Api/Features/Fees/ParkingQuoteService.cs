using Parking.Central.Data;
using Parking.Contracts;
using Parking.FeeEngine;

namespace Parking.Api.Features.Fees;

public sealed class ParkingQuoteService
{
    private readonly FeeCalculationService _feeService;
    private readonly IParkingExitRepository _parkingRepository;
    private readonly ISettlementRepository _settlementRepository;

    public ParkingQuoteService(
        FeeCalculationService feeService,
        IParkingExitRepository parkingRepository,
        ISettlementRepository settlementRepository)
    {
        _feeService = feeService;
        _parkingRepository = parkingRepository;
        _settlementRepository = settlementRepository;
    }

    public async Task<QuoteParkingFeeResponse?> QuoteByCarNumberAsync(
        int sitenum,
        string carNumber,
        DateTimeOffset exitAt,
        CancellationToken cancellationToken)
    {
        OpenParkingSessionResponse? session = await _parkingRepository.FindOpenAsync(
            sitenum,
            carNumber,
            cancellationToken);
        return session is null
            ? null
            : await QuoteAsync(session, exitAt, cancellationToken);
    }

    public async Task<QuoteParkingFeeResponse?> QuoteBySessionIdAsync(
        long parkingSessionId,
        DateTimeOffset exitAt,
        CancellationToken cancellationToken)
        => await QuoteBySessionIdAsync(parkingSessionId, exitAt, Array.Empty<int>(), cancellationToken);

    public async Task<QuoteParkingFeeResponse?> QuoteBySessionIdAsync(
        long parkingSessionId,
        DateTimeOffset exitAt,
        IReadOnlyList<int> additionalDiscountKeys,
        CancellationToken cancellationToken)
    {
        OpenParkingSessionResponse? session = await _parkingRepository.FindOpenByIdAsync(
            parkingSessionId,
            cancellationToken);
        return session is null
            ? null
            : await QuoteAsync(session, exitAt, additionalDiscountKeys, cancellationToken);
    }

    private async Task<QuoteParkingFeeResponse> QuoteAsync(
        OpenParkingSessionResponse session,
        DateTimeOffset exitAt,
        CancellationToken cancellationToken)
        => await QuoteAsync(session, exitAt, Array.Empty<int>(), cancellationToken);

    private async Task<QuoteParkingFeeResponse> QuoteAsync(
        OpenParkingSessionResponse session,
        DateTimeOffset exitAt,
        IReadOnlyList<int> additionalDiscountKeys,
        CancellationToken cancellationToken)
    {
        if (exitAt <= session.EntryAt)
            throw new ArgumentException("출차시간은 입차시간보다 늦어야 합니다.", nameof(exitAt));

        SettlementData settlement = await _settlementRepository.GetAsync(
            session.ParkingSessionId,
            cancellationToken);
        FeeCalculationResult calculation = await _feeService.CalculateSettlementAsync(
            new CalculateParkingFeeRequest
            {
                Sitenum = checked((int)session.SiteId),
                Groupnum = session.Groupnum,
                EntryAt = session.EntryAt.ToOffset(exitAt.Offset).DateTime,
                ExitAt = exitAt.DateTime,
                CarType = session.CarType,
                DiscountKeys = settlement.DiscountKeys.Concat(additionalDiscountKeys).Distinct().ToList()
            },
            cancellationToken);
        ParkingSettlementResult result = ParkingSettlementCalculator.Calculate(
            calculation.Fee.FinalFee,
            settlement.PaidAmount,
            settlement.LastPaydate,
            exitAt,
            calculation.PrepayGraceTime);

        await _parkingRepository.SaveCalculationAsync(
            session.ParkingSessionId,
            calculation.Fee.ParkingMinutes,
            calculation.Fee.OriginalFee,
            calculation.Fee.OriginalFee - calculation.Fee.FinalFee,
            calculation.Fee.FinalFee,
            cancellationToken);

        if (result.PayableAmount == 0)
            await _parkingRepository.MarkSettledAsync(
                session.ParkingSessionId,
                exitAt,
                cancellationToken);

        return new QuoteParkingFeeResponse(
            session.ParkingSessionId,
            session.CarNumber,
            session.EntryAt,
            exitAt,
            calculation.Fee,
            settlement.PaidAmount,
            result.PayableAmount,
            result.IsPrepayGrace);
    }
}
