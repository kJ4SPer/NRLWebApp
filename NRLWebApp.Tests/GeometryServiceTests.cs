using FirstWebApplication.Services;
using Xunit;

namespace NRLWebApp.Tests
{
    public class GeometryServiceTests
    {
        private readonly GeometryService _service;

        public GeometryServiceTests()
        {
            _service = new GeometryService();
        }

        [Fact]
        public void CalculateDistance_ReturnsCorrectDistance()
        {
            // Arrange
            // Oslo (approx)
            double lat1 = 59.9139;
            double lon1 = 10.7522;
            // Bergen (approx)
            double lat2 = 60.3913;
            double lon2 = 5.3221;

            // Expected distance is roughly 305 km
            // Using an online calculator for these exact coords: ~304.9 km
            double expectedDistanceMeters = 304900;

            // Act
            double result = _service.CalculateDistance(lat1, lon1, lat2, lon2);

            // Assert
            // Allow for some variance due to Earth radius constant differences
            Assert.InRange(result, expectedDistanceMeters - 2000, expectedDistanceMeters + 2000);
        }

        [Fact]
        public void CalculateDistance_SameLocation_ReturnsZero()
        {
            // Arrange
            double lat = 59.9139;
            double lon = 10.7522;

            // Act
            double result = _service.CalculateDistance(lat, lon, lat, lon);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ParseWktPoint_ValidWkt_ReturnsCoordinates()
        {
            // Arrange
            string wkt = "POINT(10.7522 59.9139)";

            // Act
            var result = _service.ParseWktPoint(wkt);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(59.9139, result.Value.lat);
            Assert.Equal(10.7522, result.Value.lng);
        }

        [Fact]
        public void ParseWktPoint_InvalidWkt_ReturnsNull()
        {
            // Arrange
            string wkt = "INVALID(10 10)";

            // Act
            var result = _service.ParseWktPoint(wkt);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseWktPoint_EmptyString_ReturnsNull()
        {
            // Arrange
            string wkt = "";

            // Act
            var result = _service.ParseWktPoint(wkt);

            // Assert
            Assert.Null(result);
        }
    }
}
