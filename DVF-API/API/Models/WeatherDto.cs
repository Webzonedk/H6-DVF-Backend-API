namespace DVF_API.API.Models
{
    public class WeatherDto
    {

        public float temperatureC { get; set; }
        public float windSpeed { get; set; }
        public float windDirection { get; set; }
        public float windGust { get; set; }
        public float relativeHumidity { get; set; }
        public float rain { get; set; }
        public float globalTiltedIrRadiance { get; set; }
        public float sunElevationAngle { get; set; }
        public float sunAzimuthAngle { get; set; }
        public DateTime dateAndTime { get; set; }

    }
}
