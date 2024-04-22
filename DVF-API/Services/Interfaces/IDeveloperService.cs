using DVF_API.Data.Models;

namespace DVF_API.Services.Interfaces
{
    public interface IDeveloperService
    {
        Task CreateHistoricWeatherDataAsync(bool createFiles, bool createDB);
        Task CreateCities();
        Task CreateLocations();
    }
}
