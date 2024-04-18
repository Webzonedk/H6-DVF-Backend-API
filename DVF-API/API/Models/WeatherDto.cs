namespace DVF_API.API.Models
{
    public class WeatherDto
    {

        public float fetchDataTimer { get; set; }
        public string address { get; set; }
        public float latitude { get; set; }
        public float longitude { get; set; }
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
