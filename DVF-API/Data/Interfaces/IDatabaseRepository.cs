using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IDatabaseRepository
    {
        Task<MetaDataDto> FetchWeatherData(SearchDto searchDto);
        Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData();
        Task InsertData(WeatherDataFromIOTDto weatherDataFromIOT);

    }
}
