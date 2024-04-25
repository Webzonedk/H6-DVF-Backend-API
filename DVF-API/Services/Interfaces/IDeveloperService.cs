using DVF_API.Data.Models;

namespace DVF_API.Services.Interfaces
{
    public interface IDeveloperService
    {
        Task CreateHistoricWeatherDataAsync(string password, string clientIp, bool createFiles, bool createDB, DateTime startDate, DateTime endDate);
        Task CreateCities(string password, string clientIp);
        Task CreateLocations(string password, string clientIp);
        Task CreateCoordinates(string password, string clientIp);
    }
}
