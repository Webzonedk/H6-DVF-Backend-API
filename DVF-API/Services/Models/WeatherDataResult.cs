using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.Models
{
    public class WeatherDataResult
    {
        public float FetchDataTimer { get; set; }
        public LocationDto Location { get; set; }
        public List<WeatherData> WeatherData { get; set; }

    }
}
