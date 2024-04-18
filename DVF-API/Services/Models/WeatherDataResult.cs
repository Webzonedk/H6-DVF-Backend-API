namespace DVF_API.Services.Models
{
    public class WeatherDataResult
    {
        public float FetchDataTimer { get; set; }
        public Location Location { get; set; }
        public List<WeatherData> WeatherData { get; set; }

    }
}
