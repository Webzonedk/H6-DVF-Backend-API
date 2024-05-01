using DVF_API.Domain.BusinessLogic;
using Xunit;

namespace DVF_API.Tests.DomainLayerTests
{
    /// <summary>
    /// This class contains tests for the SolarPositionManager class.
    /// </summary>
    public class SolarPositionManagerTests
    {




        /// <summary>
        /// Tests the CalculateSolarPosition method of the SolarPositionManager class.
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="expectedAltitude"></param>
        /// <param name="expectedAzimuth"></param>
        [Theory]
        [InlineData("2023-06-21T12:00:00Z", 51.4769, -0.0005, 61.5, 180)] // Approximate values for summer solstice in Greenwich
        [InlineData("2023-12-21T12:00:00Z", 51.4769, -0.0005, 15.0, 180)] // Approximate values for winter solstice in Greenwich
        public void CalculateSolarPosition_ReturnsExpectedValues(string dateString, double latitude, double longitude, double expectedAltitude, double expectedAzimuth)
        {
            // Arrange
            DateTime date = DateTime.Parse(dateString, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
            SolarPositionManager manager = new SolarPositionManager();

            // Act
            (double altitude, double azimuth) = manager.CalculateSolarPosition(date, latitude, longitude);

            // Assert
            Assert.InRange(altitude, expectedAltitude - 2, expectedAltitude + 2);
            Assert.InRange(azimuth, expectedAzimuth - 2, expectedAzimuth + 2);
        }
    }
}

