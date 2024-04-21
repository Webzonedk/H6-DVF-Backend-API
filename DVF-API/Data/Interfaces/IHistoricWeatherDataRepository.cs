using DVF_API.Data.Models;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(WeatherData data, string latitude, string longitude);
        Task SaveDataToDatabaseAsync(WeatherData data);
    }
}
