using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the CRUD database repository
    /// </summary>
    public interface ICrudDatabaseRepository
    {
        Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto);
        Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData();
    }
}
