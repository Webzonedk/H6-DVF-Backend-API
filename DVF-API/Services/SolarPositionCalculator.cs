using CoordinateSharp;
using System;

namespace DVF_API.Services
{
   

        /// <summary>
        /// Calculates the solar position including elevation and azimuth angles using the CoordinateSharp library.
        /// </summary>
        public class SolarPositionCalculator
        {
            /// <summary>
            /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
            /// latitude, and longitude.
            /// </summary>
            /// <param name="zuluDateTime">The date and time in Zulu (UTC).</param>
            /// <param name="latitude">The latitude in decimal degrees.</param>
            /// <param name="longitude">The longitude in decimal degrees.</param>
            /// <returns>A tuple containing the Sun's elevation angle and azimuth angle in degrees.</returns>
            public (double ElevationAngle, double AzimuthAngle) CalculateSunPosition(DateTime zuluDateTime, double latitude, double longitude)
            {
                Coordinate coordinate = new Coordinate(latitude, longitude, zuluDateTime);
                double sunElevationAngle = coordinate.CelestialInfo.SunAltitude;
                double sunAzimuthAngle = coordinate.CelestialInfo.SunAzimuth;

                return (sunElevationAngle, sunAzimuthAngle);
            }
        }


    
    // Excample of how to use the SolarPositionCalculator class
    class Program
    {
        static void Main(string[] args)
        {
            SolarPositionCalculator calculator = new SolarPositionCalculator();
            double latitude = 55.6761; // Example: Copenhagen
            double longitude = 12.5683;
            DateTime zuluDateTime = new DateTime(2024, 4, 15, 12, 0, 0, DateTimeKind.Utc); // April 15, 2024 at 12:00 UTC

            var (sunElevationAngle, sunAzimuthAngle) = calculator.CalculateSunPosition(zuluDateTime, latitude, longitude);
            Console.WriteLine($"Sun's elevation angle: {sunElevationAngle} degrees");
            Console.WriteLine($"Sun's azimuth angle: {sunAzimuthAngle} degrees");
        }
    }
}
