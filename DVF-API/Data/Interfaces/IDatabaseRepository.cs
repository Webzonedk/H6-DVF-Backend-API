using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface IDatabaseRepository
    {
        MetaDataDto FetchWeatherData(SearchDto searchDto);
        void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        void RestoreAllData();
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);

    }
}
