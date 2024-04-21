using DVF_API.Data.Models;

namespace DVF_API.Services.Interfaces
{
    internal interface IDeveloperService
    {
        void CreateHistoricWeatherDataAsync(bool createFiles, bool createDB);
        void StartSimulator();
        void StopSimulator();
        List<City> CreateCities();
        List<Location> CreateLocations();
    }
}
