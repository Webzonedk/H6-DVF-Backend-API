using CoordinateSharp;
using DVF_API.Domain.Interfaces;

namespace DVF_API.Domain.BusinessLogic
{

    /// <summary>
    /// Calculates the solar position including elevation and azimuth angles using the CoordinateSharp library.
    /// </summary>
    public class SolarPositionManager : ISolarPositionManager
    {

        #region Fields
        #endregion




        #region Constructor
        public SolarPositionManager()
        {

        }
        #endregion




        /// <summary>
        /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
        /// </summary>
        /// <param name="weatherDataDto"></param>
        /// <returns>Returns a WeatherDataDto object containing the Sun's elevation and azimuth angles in degrees.</returns>
        public (double SunAltitude, double SunAzimuth) CalculateSunAngles(DateTime dateTime, double latitude, double longitude)
        {
            return CalculateSolarPosition(dateTime, latitude, longitude);
        }




        /// <summary>
        /// Calculates the solar position including elevation and azimuth angles using the CoordinateSharp library.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns>A tuple containing the Sun's elevation and azimuth angles in degrees.</returns>
        public (double Altitude, double Azimuth) CalculateSolarPosition(DateTime dateTime, double latitude, double longitude)
        {
            // Initialize coordinate with minimal celestial calculation
            Coordinate coordinate = new Coordinate(latitude, longitude, dateTime, new EagerLoad(EagerLoadType.Celestial));
            double altitudeInDegrees = coordinate.CelestialInfo.SunAltitude;
            double azimuthInDegrees = coordinate.CelestialInfo.SunAzimuth;

            return (altitudeInDegrees, azimuthInDegrees);
        }
    }
}
