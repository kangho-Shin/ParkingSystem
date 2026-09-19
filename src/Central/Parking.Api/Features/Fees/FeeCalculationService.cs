using MySqlConnector;
using Parking.FeeEngine;

namespace Parking.Api.Features.Fees
{
    public sealed record FeeCalculationResult(
        ParkingFeeResult Fee,
        int PrepayGraceTime);

    public sealed class FeeCalculationService
    {
        private readonly string _connectionString;
        public FeeCalculationService(string connectionString) { _connectionString = connectionString; }

        public async Task<ParkingFeeResult> CalculateAsync(
            CalculateParkingFeeRequest request,
            CancellationToken cancellationToken)
        {
            await using MySqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            ParkingFeeConfiguration configuration = await LoadConfigurationAsync(
                connection,
                request.Sitenum,
                request.Groupnum,
                cancellationToken);

            ParkingFeeCalculator calculator = new(configuration);
            return calculator.Calculate(new ParkingFeeRequest
            {
                EntryAt = request.EntryAt,
                ExitAt = request.ExitAt,
                CarType = request.CarType,
                DiscountKeys = request.DiscountKeys
            });
        }

        public async Task<FeeCalculationResult> CalculateSettlementAsync(
            CalculateParkingFeeRequest request,
            CancellationToken cancellationToken)
        {
            await using MySqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            ParkingFeeConfiguration configuration = await LoadConfigurationAsync(
                connection,
                request.Sitenum,
                request.Groupnum,
                cancellationToken);

            ParkingFeeResult fee = new ParkingFeeCalculator(configuration).Calculate(
                new ParkingFeeRequest
                {
                    EntryAt = request.EntryAt,
                    ExitAt = request.ExitAt,
                    CarType = request.CarType,
                    DiscountKeys = request.DiscountKeys
                });

            return new FeeCalculationResult(fee, configuration.PrepayGraceTime);
        }

        private static Task<ParkingFeeConfiguration> LoadConfigurationAsync(
            MySqlConnection connection,
            int sitenum,
            int groupnum,
            CancellationToken cancellationToken)
        {
            ParkCalcConfig configLoader = new();
            return configLoader.LoadAsync(
                connection,
                sitenum,
                groupnum,
                cancellationToken);
        }
    }
}
