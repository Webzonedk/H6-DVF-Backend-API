using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(List<SaveFileDto> _saveToFileDtoList, string baseFolder);
        Task SaveDataToDatabaseAsync(List<SaveFileDto> _saveToFileDtoList);
        Task InsertLocationsToDB(List<LocationDto> locations);
        Task InsertCitiesToDB(List<City> cities);
    }
}
