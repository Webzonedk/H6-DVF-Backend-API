using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;
using System.Globalization;
using System.Text.Json;

namespace DVF_API.Data.Repositories
{
    public class HistoricWeatherDataRepository : IHistoricWeatherDataRepository
    {
        private readonly IConfiguration configuration;
        private readonly string connectionString;

        public HistoricWeatherDataRepository(IConfiguration _configuration)
        {

            configuration = _configuration;
            connectionString = configuration.GetConnectionString("WeatherDataDb");
        }

        private readonly string _baseFolder;

        public HistoricWeatherDataRepository(string baseFolder)
        {
            _baseFolder = baseFolder;
        }




        public async Task SaveDataToFileAsync(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            await SaveDataAsBinaryAsync(data, latitude, longitude, baseFolder);
        }





        public async Task SaveDataToDatabaseAsync(WeatherData data)
        {
            
        
        }




        public void SaveCitiesToDB(List<City> cities)
        {
          
        }




        public void SaveLocationsToDB(List<Location> locations)
        {
        
        }


        private async Task SaveDataAsBinaryAsync(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            var groupedData = data.Hourly.Time
                                    .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
                                    .GroupBy(t => t.Time.ToString("yyyyMMdd"))
                                    .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var entry in groupedData)
            {
                string dateKey = entry.Key;
                DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
                string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

                if (!Directory.Exists(yearFolder))
                    Directory.CreateDirectory(yearFolder);

                string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");  // End with .bin to indicate binary file

                using (var binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    foreach (var v in entry.Value)
                    {
                        // Convert each time point to float and write directly as binary
                        await Task.Run(() => {
                            binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[v.Index]));
                            binWriter.Write(data.Hourly.Temperature_2m[v.Index]);
                            binWriter.Write(data.Hourly.Relative_Humidity_2m[v.Index]);
                            binWriter.Write(data.Hourly.Rain[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Speed_10m[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Direction_10m[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Gusts_10m[v.Index]);
                            binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]);
                        });
                    }
                }
            }
        }



        private float ConvertDateTimeToFloat(string time)
        {
            DateTime parsedDateTime = DateTime.Parse(time);
            return float.Parse(parsedDateTime.ToString("HHmm"));
        }



        private void SaveDataAsBinary(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            var groupedData = data.Hourly.Time
                                .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
                                .GroupBy(t => t.Time.ToString("yyyyMMdd"))
                                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var entry in groupedData)
            {
                string dateKey = entry.Key;
                DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
                string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

                if (!Directory.Exists(yearFolder))
                    Directory.CreateDirectory(yearFolder);

                string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");  // End with .bin to indicate binary file

                using (var binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    foreach (var v in entry.Value)
                    {
                        // Convert each time point to float and write directly as binary
                        binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[v.Index]));
                        binWriter.Write(data.Hourly.Temperature_2m[v.Index]);
                        binWriter.Write(data.Hourly.Relative_Humidity_2m[v.Index]);
                        binWriter.Write(data.Hourly.Rain[v.Index]);
                        binWriter.Write(data.Hourly.Wind_Speed_10m[v.Index]);
                        binWriter.Write(data.Hourly.Wind_Direction_10m[v.Index]);
                        binWriter.Write(data.Hourly.Wind_Gusts_10m[v.Index]);
                        binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]);
                    }
                }
            }
        }

    
    }
}
