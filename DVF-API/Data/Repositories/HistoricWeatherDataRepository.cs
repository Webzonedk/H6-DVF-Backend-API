using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DVF_API.Data.Repositories
{
    public class HistoricWeatherDataRepository : IHistoricWeatherDataRepository
    {
        private readonly DbContext _context;
        private readonly string _baseFolder;

        public HistoricWeatherDataRepository(DbContext context, string baseFolder)
        {
            _context = context;
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
            _context.Add(data);
            await _context.SaveChangesAsync();
        }
    }
}
