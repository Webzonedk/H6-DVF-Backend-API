using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the data service
    /// </summary>
    public interface IDataService
    {
        Task<List<string>> GetAddressesFromDBMatchingInputs(string partialAddress);
        Task<int> CountLocations();
        Task<Dictionary<long, string>> GetLocationCoordinates(int fromIndex, int toIndex);
        Task<MetaDataDto> GetWeatherDataService(SearchDto seachDto);
    }
}
