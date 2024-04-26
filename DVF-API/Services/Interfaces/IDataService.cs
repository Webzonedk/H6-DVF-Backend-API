using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.Interfaces
{
    public interface IDataService
    {
        Task<List<string>> GetAddressesFromDBMatchingInputs(string partialAddress);
        Task<int> CountLocations();
        Task<Dictionary<int, string>> GetLocationCoordinates(int fromIndex, int toIndex);
        Task<MetaDataDto> GetWeatherDataService(SearchDto seachDto);
    }
}
