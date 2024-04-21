namespace DVF_API.Services.Models
{
    public class WeatherDataFromIOT
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public float Temperature { get; set; }
        public float WindSpeed { get; set; }
        public float WindDirection { get; set; }
        public float WindGust { get; set; }
        public float RelativeHumidity { get; set; }
        public float Rain { get; set; }
        public float GlobalTiltedIrRadiance { get; set; }
        public string DateAndTime { get; set; }
    }
}
