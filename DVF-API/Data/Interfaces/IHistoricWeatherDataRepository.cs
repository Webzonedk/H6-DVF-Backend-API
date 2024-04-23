using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(List<SaveToStorageDto> _saveToFileDtoList, string baseFolder);
        Task SaveDataToDatabaseAsync(List<SaveToStorageDto> _saveToFileDtoList);
        Task SaveLocationsToDBAsync(List<LocationDto> locations);
        Task SaveCitiesToDBAsync(List<City> cities);
        Task SaveCoordinatesToDBAsync(List<string> coordinates);
    }
}
