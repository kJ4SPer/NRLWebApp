using System.Globalization;
using System.Text.RegularExpressions;

namespace FirstWebApplication.Services
{
    public class GeometryService
    {
        // Beregner avstand mellom to koordinater med Haversine-formelen
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMeters = 6371000;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        // Konverterer grader til radianer
        public double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        // Parser WKT POINT-string til koordinater
        public (double lat, double lng)? ParseWktPoint(string? wkt)
        {
            if (string.IsNullOrEmpty(wkt))
                return null;

            try
            {
                // Format: POINT(longitude latitude)
                var match = Regex.Match(
                    wkt,
                    @"POINT\s*\(\s*([\d\.\-]+)\s+([\d\.\-]+)\s*\)");

                if (match.Success)
                {
                    var lng = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var lat = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    return (lat, lng);
                }
            }
            catch { }

            return null;
        }
    }
}
