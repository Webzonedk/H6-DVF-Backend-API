using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface ICrudFileRepository
    {
        Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(SearchDto search);
        Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData();
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);
    }
}
