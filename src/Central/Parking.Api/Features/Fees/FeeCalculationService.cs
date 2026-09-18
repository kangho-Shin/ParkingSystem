using MySqlConnector;
using Parking.FeeEngine;

namespace Parking.Api.Features.Fees
{
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

            ParkCalcConfig configLoader = new();
            ParkingFeeConfiguration configuration = await configLoader.LoadAsync(
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
    }
}
