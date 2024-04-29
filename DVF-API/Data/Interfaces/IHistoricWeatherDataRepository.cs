using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(string fileName, WeatherStruct[] weatherStruct);
        Task SaveDataToDatabaseAsync(DateTime date, WeatherStruct[] veatherStruct);
        Task SaveLocationsToDBAsync(List<LocationDto> locations);
        Task SaveCitiesToDBAsync(List<City> cities);
        Task SaveCoordinatesToDBAsync(List<string> coordinates);
    }
}
