using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the location repository
    /// </summary>
    public interface ILocationRepository
    {
        Task<List<string>> FetchMatchingAddresses(string partialAddress);
        Task<int> FetchLocationCount();
        Task<Dictionary<long, string>> FetchLocationCoordinates(int fromIndex, int toIndex);
        Task<List<BinaryDataFromFileDto>> FetchAddressByCoordinates(SearchDto searchDto);
        Task<Dictionary<long, LocationDto>> GetAllLocationCoordinates();
    }
}
