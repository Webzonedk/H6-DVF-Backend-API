using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder);
        Task SaveDataToDatabaseAsync(WeatherData data);
        Task InsertLocationsToDB(List<Location> locations);
        Task InsertCitiesToDB(List<City> cities);
    }
}
