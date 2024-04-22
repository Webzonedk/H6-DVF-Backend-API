using DVF_API.Services.Models;

namespace DVF_API.Services.Interfaces
{
    public interface IAddWeatherDataService
    {
        public void ApplyWeatherData(WeatherDataFromIOT weatherDataFromIOTDto);
    }
}
