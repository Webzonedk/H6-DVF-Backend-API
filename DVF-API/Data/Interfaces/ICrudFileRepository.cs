using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the CRUD file repository
    /// </summary>
    public interface ICrudFileRepository
    {
        Task<BinaryWeatherStructDto[]> FetchWeatherDataAsync(BinarySearchInFilesDto binarySearchInFilesDtos);
        Task DeleteOldData(string baseDirectory, string deletedFilesDirectory, DateTime deleteWeatherDataBeforeThisDate);
        Task RestoreAllData(string baseDirectory, string deletedFilesDirectory);
    }
}
