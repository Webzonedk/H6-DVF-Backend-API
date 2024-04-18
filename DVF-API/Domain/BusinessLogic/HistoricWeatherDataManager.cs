using DVF_API.Services.Models;
using System.Text.Json;

namespace DVF_API.Domain.BusinessLogic
{
    public class HistoricWeatherDataManager
    {

        private HttpClient _client = new HttpClient();
        //private string _baseFolder = Path.Combine("/app/data/weatherData");
        // Set the base folder to a local path for testing on computers desktop
        private string _baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "weatherData");
        private double _latitude = 55.3235;
        private double _longitude = 11.9639;
        private DateTime _startDate = new DateTime(2024, 03, 30);
        private DateTime _endDate = new DateTime(2024, 04, 01);


        public async Task FetchAndStoreWeatherData(double _latitude, double _longitude, DateTime _startDate, DateTime _endDate)
        {
            string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={_latitude}&longitude={_longitude}&start_date={_startDate:yyyy-MM-dd}&end_date={_endDate:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";

            try
            {
                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonData = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<WeatherData>(jsonData);

                SaveData(weatherData, _latitude, _longitude);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }




        private void SaveData(WeatherData data, double latitude, double longitude)
        {
            foreach (var entry in data.Hourly.Time)
            {
                DateTime entryDate = DateTime.Parse(entry);
                string datePath = Path.Combine(_baseFolder, $"{latitude}-{longitude}", entryDate.Year.ToString(), entryDate.ToString("yyyy-MM-dd"));

                if (!Directory.Exists(datePath))
                    Directory.CreateDirectory(datePath);

                string filePath = Path.Combine(datePath, $"{entryDate.Hour}.bin");

                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
                    fs.Write(jsonBytes, 0, jsonBytes.Length);
                }
            }
        }

        public class WeatherData
        {
            public HourlyData Hourly { get; set; }
        }

        public class HourlyData
        {
            public string[] Time { get; set; }
            public float[] Temperature_2m { get; set; }
            public float[] Relative_humidity_2m { get; set; }
            public float[] Rain { get; set; }
            public float[] Wind_speed_10m { get; set; }
            public float[] Wind_direction_10m { get; set; }
            public float[] Wind_gusts_10m { get; set; }

        }
    }
}
