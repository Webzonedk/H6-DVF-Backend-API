using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(string fileName, byte[] byteArrayToSaveToFile);
        Task SaveDataToDatabaseAsync(List<SaveToStorageDto> _saveToFileDtoList);
        Task SaveLocationsToDBAsync(List<LocationDto> locations);
        Task SaveCitiesToDBAsync(List<City> cities);
        Task SaveCoordinatesToDBAsync(List<string> coordinates);
    }
}
