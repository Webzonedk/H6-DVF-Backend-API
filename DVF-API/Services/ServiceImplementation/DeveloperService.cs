using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace DVF_API.Services.ServiceImplementation
{
    public class DeveloperService : IDeveloperService
    {
        private readonly IDeveloperService _developerService;

        private readonly HttpClient _client = new HttpClient();
#if DEBUG
        private string _baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "weatherData\\");
#else
        private string _baseFolder = "/app/data/weatherData/";
#endif
        private string _coordinatesFilePath = "..\\DVF-API\\Sources\\uniqueCoordinatesSelected.json";
        private string _latitude = "55.3235";
        private string _longitude = "11.9639";
        private DateTime _startDate = new DateTime(2024, 03, 30);
        private DateTime _endDate = new DateTime(2024, 04, 01);
        private HistoricWeatherDataDto? _originalWeatherData;


        #region fields
        private readonly IHistoricWeatherDataRepository _historicWeatherDataRepository;
        #endregion

        #region Constructor
        internal DeveloperService(IHistoricWeatherDataRepository historicWeatherDataRepository)
        {
            _historicWeatherDataRepository = historicWeatherDataRepository;
        }
        internal DeveloperService(IDeveloperService developerService)
        {
            _developerService = developerService;
        }
        #endregion



        public void CreateHistoricWeatherDataAsync(bool createFiles, bool createDB)
        {
            throw new System.NotImplementedException();
        }

        public void StartSimulator()
        {
            throw new System.NotImplementedException();
        }


        public void StopSimulator()
        {
            throw new System.NotImplementedException();
        }




        public void CreateHistoricWeatherData()
        {
            Task.Run(async () =>
            {
                try
                {
                    await GenerateAndSaveOriginalData(_latitude, _longitude, _startDate, _endDate, _baseFolder);
                    await ProcessAllCoordinates(_coordinatesFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An error occurred: " + ex.Message);
                }
                finally
                {
                    Debug.WriteLine("Historical weather data generation completed.");
                }
            });
        }




        private async Task GenerateAndSaveOriginalData(string latitude, string longitude, DateTime startDate, DateTime endDate, string baseFolder)
        {
            string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonData = await response.Content.ReadAsStringAsync();
            _originalWeatherData = JsonSerializer.Deserialize<HistoricWeatherDataDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (_originalWeatherData != null)
            {
                var formatedLatitude = FormatCoordinate(latitude);
                var formatedLongitude = FormatCoordinate(longitude);
                await _historicWeatherDataRepository.SaveDataToFileAsync(_originalWeatherData, formatedLatitude, formatedLongitude, baseFolder);
            }
        }




        private HistoricWeatherDataDto? ModifyData(HistoricWeatherDataDto? originalData)
        {
            if (originalData == null)
                return null;
            var random = new Random();
            HistoricWeatherDataDto modifiedData = new HistoricWeatherDataDto
            {
                Hourly = new HourlyData
                {
                    Time = originalData.Hourly.Time,
                    Temperature_2m = originalData.Hourly.Temperature_2m.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Relative_Humidity_2m = originalData.Hourly.Relative_Humidity_2m.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Rain = originalData.Hourly.Rain.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Wind_Speed_10m = originalData.Hourly.Wind_Speed_10m.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Wind_Direction_10m = originalData.Hourly.Wind_Direction_10m.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Wind_Gusts_10m = originalData.Hourly.Wind_Gusts_10m.Select(x => AdjustValueRandomly(x, random)).ToArray(),
                    Global_Tilted_Irradiance_Instant = originalData.Hourly.Global_Tilted_Irradiance_Instant.Select(x => AdjustValueRandomly(x, random)).ToArray()
                }
            };
            return modifiedData;
        }




        public async Task ProcessAllCoordinates(string coordinatesFilePath)
        {
            var coordinates = ReadCoordinates(coordinatesFilePath);
            foreach (var coordinate in coordinates)
            {
                string[] parts = coordinate.Split('-');
                if (parts.Length == 2 && (parts[0] != _latitude || parts[1] != _longitude))  // Skip the original coordinates
                {
                    HistoricWeatherDataDto? modifiedData = ModifyData(_originalWeatherData);
                    if (modifiedData != null)
                    {
                        await _historicWeatherDataRepository.SaveDataToFileAsync(modifiedData, parts[0], parts[1], _baseFolder);
                    }
                }
            }
        }



        private IEnumerable<string> ReadCoordinates(string filePath)
        {
            var coordinatesText = File.ReadAllLines(filePath);
            foreach (var line in coordinatesText)
            {
                string trimmedLine = line.Trim(new char[] { '\"', ',', ' ' });

                var parts = trimmedLine.Split('-');
                if (parts.Length == 2)
                {
                    string latitude = FormatCoordinate(parts[0]);
                    string longitude = FormatCoordinate(parts[1]);
                    yield return $"{latitude}-{longitude}";
                }
            }
        }



        private string FormatCoordinate(string coordinate)
        {
            var parts = coordinate.Split('.');
            if (parts.Length == 2)
            {
                string integerPart = parts[0].PadLeft(2, '0');
                string decimalPart = parts[1].PadRight(8, '0');
                return $"{integerPart}.{decimalPart}";
            }
            return coordinate;
        }



        private float AdjustValueRandomly(float originalValue, Random random)
        {
            double percentage = (random.NextDouble() * 0.6 - 0.3);  // Adjust by up to ±30%
            float newValue = (float)Math.Round(originalValue * (1 + percentage), 2, MidpointRounding.AwayFromZero);
            return newValue;
        }


        //private void SaveDataAsBinary(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        //{
        //    var groupedData = data.Hourly.Time
        //                        .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
        //                        .GroupBy(t => t.Time.ToString("yyyyMMdd"))
        //                        .ToDictionary(g => g.Key, g => g.ToList());

        //    foreach (var entry in groupedData)
        //    {
        //        string dateKey = entry.Key;
        //        DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
        //        string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

        //        if (!Directory.Exists(yearFolder))
        //            Directory.CreateDirectory(yearFolder);

        //        string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");  // End with .bin to indicate binary file

        //        using (var binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        //        {
        //            foreach (var v in entry.Value)
        //            {
        //                // Convert each time point to float and write directly as binary
        //                binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[v.Index]));
        //                binWriter.Write(data.Hourly.Temperature_2m[v.Index]);
        //                binWriter.Write(data.Hourly.Relative_Humidity_2m[v.Index]);
        //                binWriter.Write(data.Hourly.Rain[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Speed_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Direction_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Gusts_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]);
        //            }
        //        }
        //    }
        //}



        //private void SaveData(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        //{


        //    var groupedData = data.Hourly.Time
        //                        .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
        //                        .GroupBy(t => t.Time.ToString("yyyyMMdd"))
        //                        .ToDictionary(g => g.Key, g => g.ToList());


        //    foreach (var entry in groupedData)
        //    {
        //        string dateKey = entry.Key;
        //        DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
        //        string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

        //        if (!Directory.Exists(yearFolder))
        //            Directory.CreateDirectory(yearFolder);

        //        string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.json");

        //        HistoricWeatherDataOutputDto dailyData = new HistoricWeatherDataOutputDto
        //        {
        //            Hourly = new HourlyDataOutput
        //            {
        //                Time = entry.Value.Select(v => ConvertDateTimeToFloat(data.Hourly.Time[v.Index])).ToArray(),
        //                Temperature_2m = entry.Value.Select(v => data.Hourly.Temperature_2m[v.Index]).ToArray(),
        //                Relative_Humidity_2m = entry.Value.Select(v => data.Hourly.Relative_Humidity_2m[v.Index]).ToArray(),
        //                Rain = entry.Value.Select(v => data.Hourly.Rain[v.Index]).ToArray(),
        //                Wind_Speed_10m = entry.Value.Select(v => data.Hourly.Wind_Speed_10m[v.Index]).ToArray(),
        //                Wind_Direction_10m = entry.Value.Select(v => data.Hourly.Wind_Direction_10m[v.Index]).ToArray(),
        //                Wind_Gusts_10m = entry.Value.Select(v => data.Hourly.Wind_Gusts_10m[v.Index]).ToArray(),
        //                Global_Tilted_Irradiance_Instant = entry.Value.Select(v => data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]).ToArray()
        //            }
        //        };

        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        string json = JsonSerializer.Serialize(dailyData, options);
        //        File.WriteAllText(filePath, json);
        //    }
        //}




        //private float ConvertDateTimeToFloat(string time)
        //{
        //    DateTime parsedDateTime = DateTime.Parse(time);
        //    return float.Parse(parsedDateTime.ToString("HHmm"));
        //}









    }
}
