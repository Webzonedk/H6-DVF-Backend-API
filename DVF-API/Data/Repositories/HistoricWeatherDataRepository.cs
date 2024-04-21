using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using System.Text.Json;

namespace DVF_API.Data.Repositories
{
    public class HistoricWeatherDataRepository : IHistoricWeatherDataRepository
    {
        private readonly DvfDbContext _dvfDbContext;
        private readonly string _baseFolder;

        public HistoricWeatherDataRepository(DvfDbContext dvfDbContext, string baseFolder)
        {
            _dvfDbContext = dvfDbContext;
            _baseFolder = baseFolder;
        }
        public async Task SaveDataToFileAsync(WeatherData data, string latitude, string longitude)
        {
            string directoryPath = Path.Combine(_baseFolder, latitude + "-" + longitude);
            Directory.CreateDirectory(directoryPath);

            string filePath = Path.Combine(directoryPath, $"{DateTime.Now:yyyyMMddHHmmss}.json");
            string jsonData = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        public async Task SaveDataToDatabaseAsync(WeatherData data)
        {
            _dvfDbContext.Add(data);
            await _dvfDbContext.SaveChangesAsync();
        }

        public void SaveCitiesToDB(List<City> cities)
        {
            _dvfDbContext.AddRange(cities);
            _dvfDbContext.SaveChanges();
        }


        public void SaveLocationsToDB(List<Location> locations)
        {
            _dvfDbContext.AddRange(locations);
            _dvfDbContext.SaveChanges();
        }
    }
}
