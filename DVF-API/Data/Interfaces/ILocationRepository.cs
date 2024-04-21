namespace DVF_API.Data.Interfaces
{
    public interface ILocationRepository
    {
        List<string> FetchMatchingAddresses(string partialAddress);
        int FetchLocationCount(string partialAddress);
        List<string> FetchLoactionCoordinates(int fromIndex, int toIndex);
    }
}
