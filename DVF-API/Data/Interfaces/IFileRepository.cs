using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IFileRepository
    {
        List<byte[]> FetchWeatherData(SearchDto search);
        void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        void RestoreAllData();
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);
    }
}
