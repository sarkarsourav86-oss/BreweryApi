using BreweryApi.Application.Interfaces;

namespace BreweryApi.Infrastructure.Services;

public sealed class DistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusMiles = 3958.8;

    public double CalculateMiles(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var originLat = ToRadians(lat1);
        var destinationLat = ToRadians(lat2);

        var a =
            Math.Pow(Math.Sin(dLat / 2), 2) +
            Math.Cos(originLat) * Math.Cos(destinationLat) *
            Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));

        return EarthRadiusMiles * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180d);
}