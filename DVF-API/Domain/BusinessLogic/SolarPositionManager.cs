using CoordinateSharp;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System;

namespace DVF_API.Domain.BusinessLogic
{


    /// <summary>
    /// Calculates the solar position including elevation and azimuth angles using the CoordinateSharp library.
    /// </summary>
    public class SolarPositionManager: ISolarPositionManager
    {



        public SolarPositionManager()
        {
            
        }



        /// <summary>
        /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
        /// </summary>
        /// <param name="weatherDataDto"></param>
        /// <returns>Returns a WeatherDataDto object containing the Sun's elevation and azimuth angles in degrees.</returns>
        public WeatherDataDto CalculateSunAngles(WeatherDataDto weatherDataDto) 
        {
            return CalculateSunPosition(weatherDataDto);
        }




        /// <summary>
        /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
        /// latitude, and longitude and adding the values to the WeatherDataDto object.
        /// </summary>
        /// <param name="zuluDateTime">The date and time in Zulu (UTC).</param>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <returns>an WeatherDataDto object containing the Sun's elevation and azimuth angles in degrees.</returns>
        private WeatherDataDto CalculateSunPosition(WeatherDataDto weathterDataDto)
        {
           // double latitude = Math.Round(Convert.ToDouble(weathterDataDto.Latitude), 8, MidpointRounding.AwayFromZero);
           string formattedLat = weathterDataDto.Latitude.Replace(".",",");
            string formattedLong = weathterDataDto.Longitude.Replace(".", ",");
            //double latitude = double.Parse(formattedLat);
            //double longitude = double.Parse(formattedLong);
            double latitude = Math.Round(double.Parse(formattedLat), 8, MidpointRounding.AwayFromZero);
            double longitude = Math.Round(double.Parse(formattedLong), 8, MidpointRounding.AwayFromZero);

            Coordinate coordinate = new Coordinate(latitude, longitude, weathterDataDto.DateAndTime);
            weathterDataDto.SunElevationAngle = (float)coordinate.CelestialInfo.SunAltitude;
            weathterDataDto.SunAzimuthAngle = (float)coordinate.CelestialInfo.SunAzimuth;

            return (weathterDataDto);
        }

    }
}
