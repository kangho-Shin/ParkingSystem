using System.Globalization;
using System.Text;

namespace Parking.Contracts;

public static class VehicleImageName
{
    private static readonly HashSet<char> InvalidCharacters =
        ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public static string Create(
        long siteId,
        int groupnum,
        long laneId,
        string eventType,
        DateTimeOffset eventDateTime,
        string carNumber,
        Guid eventId)
    {
        string safeCarNumber = Sanitize(carNumber);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{siteId:D3}_{groupnum:D3}_{laneId:D3}_{eventType}_{eventDateTime:yyyyMMddHHmmssfff}_{safeCarNumber}_{eventId:N}.jpg");
    }

    private static string Sanitize(string value)
    {
        StringBuilder result = new(value.Length);

        foreach (char character in value)
        {
            if (!char.IsControl(character) && !InvalidCharacters.Contains(character))
                result.Append(character);
        }

        return result.ToString();
    }
}
