using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.Interfaces
{
    internal interface IDataService
    {
        List<string> GetAddressesFromDBMatchingInputs(string partialAddress);
        int CountLocations();
        List<string> GetLocationCoordinates(int fromIndex, int toIndex);
        MetaDataDto GetWeatherDataService(SearchDto seachDto);
    }
}
