namespace IOT_Simulator.Models
{
    public class WeatherDataDtoIOT
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public float temperatureC { get; set; }
        public float windDirection { get; set; }
        public float windGusts { get; set; }
        public float relativeHumidity { get; set; }
        public float rain { get; set; }
        public float globalTiltedIrRadiance { get; set; }
        public DateTime dateAndTime { get; set; }
    }
}
