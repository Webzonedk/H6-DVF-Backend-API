using DVF_API.Services.Models;

namespace DVF_API.Services.Interfaces
{
    internal interface IAddWeatherDataService
    {
        public void ApplyWeatherData(WeatherDataFromIOT weatherDataFromIOTDto);
    }
}
