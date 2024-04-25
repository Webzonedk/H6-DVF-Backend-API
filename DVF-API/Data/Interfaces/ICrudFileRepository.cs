using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface ICrudFileRepository
    {
        Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(string baseDirectory, SearchDto search);
        Task DeleteOldData(string baseDirectory, string deletedFilesDirectory, DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData(string baseDirectory, string deletedFilesDirectory);
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);
    }
}
