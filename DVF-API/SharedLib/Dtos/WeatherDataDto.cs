namespace DVF_API.SharedLib.Dtos
{
    public class WeatherDataDto
    {


        public string? Address { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
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
