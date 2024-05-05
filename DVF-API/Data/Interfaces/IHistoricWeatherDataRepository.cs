using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the historic weather data repository
    /// </summary>
    public interface IHistoricWeatherDataRepository
    {
        Task SaveDataToFileAsync(string fileName, BinaryWeatherStructDto[] weatherStruct);
        Task<bool> SaveDataToDatabaseAsync(DateTime date, BinaryWeatherStructDto[] veatherStruct);
        Task SaveLocationsToDBAsync(List<LocationDto> locations);
        Task SaveCitiesToDBAsync(List<City> cities);
        Task SaveCoordinatesToDBAsync(List<string> coordinates);
    }
}
