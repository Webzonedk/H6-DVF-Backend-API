using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface ICrudDatabaseRepository
    {
        Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto);
        Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData();
        Task InsertData(WeatherDataFromIOTDto weatherDataFromIOT);

    }
}
