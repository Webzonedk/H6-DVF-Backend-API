namespace DVF_API.Services.Interfaces
{
    /// <summary>
    /// This interface connects the service layer with the developer manager
    /// </summary>
    public interface IDeveloperService
    {
        Task CreateHistoricWeatherDataAsync(string password, string clientIp, bool createFiles, bool createDB, DateTime startDate, DateTime endDate);
        Task CreateCities(string password, string clientIp);
        Task CreateLocations(string password, string clientIp);
        Task CreateCoordinates(string password, string clientIp);
    }
}
