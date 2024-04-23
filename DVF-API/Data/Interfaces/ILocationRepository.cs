namespace DVF_API.Data.Interfaces
{
    public interface ILocationRepository
    {
        Task<List<string>> FetchMatchingAddresses(string partialAddress);
        Task<int> FetchLocationCount();
        Task<List<string>> FetchLocationCoordinates(int fromIndex, int toIndex);
    }
}
