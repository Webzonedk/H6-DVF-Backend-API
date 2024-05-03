﻿namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This class is used to store weather data to be sent to the client as a part of MetaDataDto.
    /// </summary>
    public class WeatherDataDto
    {
        public string? Address { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public float TemperatureC { get; set; }
        public float WindSpeed { get; set; }
        public float WindDirection { get; set; }
        public float WindGust { get; set; }
        public float RelativeHumidity { get; set; }
        public float Rain { get; set; }
        public float GlobalTiltedIrRadiance { get; set; }
        public float SunElevationAngle { get; set; }
        public float SunAzimuthAngle { get; set; }
        public DateTime DateAndTime { get; set; }
    }
}
