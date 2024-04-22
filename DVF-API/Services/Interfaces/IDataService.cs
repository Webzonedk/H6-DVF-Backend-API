using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.Interfaces
{
    public interface IDataService
    {
        List<string> GetAddressesFromDBMatchingInputs(string partialAddress);
        int CountLocations();
        List<string> GetLocationCoordinates(int fromIndex, int toIndex);
        MetaDataDto GetWeatherDataService(SearchDto seachDto);
    }
}
