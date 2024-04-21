using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IDataRepository
    {
        MetaDataDto FetchWeatherData(SearchDto searchDto);
        List<string> FetchMatchingAddresses(string partialAddress);
        int FetchLocationCount(string partialAddress);
        List<string> FetchLoactionCoordinates(int fromIndex, int toIndex);
        void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        void RestoreAllData();
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);

    }
}
