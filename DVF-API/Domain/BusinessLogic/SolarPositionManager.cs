using CoordinateSharp;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System;

namespace DVF_API.Domain.BusinessLogic
{


    /// <summary>
    /// Calculates the solar position including elevation and azimuth angles using the CoordinateSharp library.
    /// </summary>
    public class SolarPositionManager : ISolarPositionManager
    {
        Coordinate coordinate;


        public SolarPositionManager()
        {

        }



        /// <summary>
        /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
        /// </summary>
        /// <param name="weatherDataDto"></param>
        /// <returns>Returns a WeatherDataDto object containing the Sun's elevation and azimuth angles in degrees.</returns>
        public (double SunAltitude, double SunAzimuth) CalculateSunAngles(DateTime dateTime, double latitude, double longitude)
        {
            return CalculateSunPosition(dateTime, latitude, longitude);
        }




        /// <summary>
        /// Calculates the Sun's position including elevation and azimuth angles for a given Zulu (UTC) date and time,
        /// latitude, and longitude and adding the values to the WeatherDataDto object.
        /// </summary>
        /// <param name="zuluDateTime">The date and time in Zulu (UTC).</param>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <returns>an WeatherDataDto object containing the Sun's elevation and azimuth angles in degrees.</returns>
       
        //old method
        //private WeatherDataDto CalculateSunPosition(WeatherDataDto weathterDataDto)
        //{
        //    // double latitude = Math.Round(Convert.ToDouble(weathterDataDto.Latitude), 8, MidpointRounding.AwayFromZero);
        //    string formattedLat = weathterDataDto.Latitude!.Replace(".", ",");
        //    string formattedLong = weathterDataDto.Longitude!.Replace(".", ",");
        //    //double latitude = double.Parse(formattedLat);
        //    //double longitude = double.Parse(formattedLong);
        //    double latitude = Math.Round(double.Parse(formattedLat), 8, MidpointRounding.AwayFromZero);
        //    double longitude = Math.Round(double.Parse(formattedLong), 8, MidpointRounding.AwayFromZero);

        //    if (coordinate == null)
        //    {
        //        coordinate = new Coordinate(latitude, longitude, weathterDataDto.DateAndTime);

        //    }
        //    else
        //    {
        //        coordinate.Latitude.DecimalDegree = latitude;
        //        coordinate.Longitude.DecimalDegree = longitude;
        //        coordinate.GeoDate = weathterDataDto.DateAndTime;
        //    }
        //    weathterDataDto.SunElevationAngle = (float)coordinate.CelestialInfo.SunAltitude;
        //    weathterDataDto.SunAzimuthAngle = (float)coordinate.CelestialInfo.SunAzimuth;

        //    return (weathterDataDto);
        //}

        public (double SunAltitude, double SunAzimuth) CalculateSunPosition(DateTime dateTime, double latitude, double longitude)
        {
            // Constants
            double deg2Rad = Math.PI / 180.0;
            double rad2Deg = 180.0 / Math.PI;

            // Convert local time into Julian date
            double julianDate = dateTime.ToOADate() + 2415018.5;

            // Calculate declination and equation of time
            double time = julianDate - 2451545.0;
            double meanLongitude = (280.460 + 0.9856474 * time) % 360;
            double meanAnomaly = (357.528 + 0.9856003 * time) % 360;
            double eclipticLongitude = meanLongitude + 1.915 * Math.Sin(meanAnomaly * deg2Rad) + 0.020 * Math.Sin(2 * meanAnomaly * deg2Rad);
            double obliquityOfEcliptic = 23.439 - 0.0000004 * time;
            double declination = Math.Asin(Math.Sin(obliquityOfEcliptic * deg2Rad) * Math.Sin(eclipticLongitude * deg2Rad)) * rad2Deg;

            // Hour angle
            double localSiderealTime = (100.46 + 0.985647 * time + longitude + 15 * dateTime.TimeOfDay.TotalHours) % 360;
            double hourAngle = (localSiderealTime - declination) * deg2Rad;

            // Solar position
            double sunAltitude = Math.Asin(Math.Sin(latitude * deg2Rad) * Math.Sin(declination * deg2Rad) + Math.Cos(latitude * deg2Rad) * Math.Cos(declination * deg2Rad) * Math.Cos(hourAngle)) * rad2Deg;
            double sunAzimuth = Math.Acos((Math.Sin(declination * deg2Rad) - Math.Sin(sunAltitude * deg2Rad) * Math.Sin(latitude * deg2Rad)) / (Math.Cos(sunAltitude * deg2Rad) * Math.Cos(latitude * deg2Rad))) * rad2Deg;

            // Correction for azimuth angle
            if (hourAngle > 0) sunAzimuth = 360 - sunAzimuth;

            return (sunAltitude, sunAzimuth);
        }




    }
}
