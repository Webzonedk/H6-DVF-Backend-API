using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace DVF_API.Services.ServiceImplementation
{
    public class DeveloperService : IDeveloperService
    {

        #region fields
        private readonly HttpClient _httpClient = new HttpClient();

        //private string _baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "weatherData\\");
        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/deletedWeatherData/";

        //private string _coordinatesFilePath = "..\\DVF-API\\Sources\\UniqueCoordinatesSelected.json";
        private string _coordinatesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "UniqueCoordinatesSelected.json");
        private string _latitude = "55.3235";
        private string _longitude = "11.9639";
        private DateTime _startDate = new DateTime(2024, 04, 01);
        private DateTime _endDate = new DateTime(2024, 04, 01);
        private List<SaveToStorageDto> _saveToStorageDto = new List<SaveToStorageDto>();
        private const string VerySecretPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private static Dictionary<string, (DateTime lastAttempt, int attemptCount)> _loginAttempts = new();



        private readonly IHistoricWeatherDataRepository _historicWeatherDataRepository;
        private readonly IDatabaseRepository _databaseRepository;
        private readonly IFileRepository _fileRepository;
        #endregion




        #region Constructors

        public DeveloperService(IHistoricWeatherDataRepository historicWeatherDataRepository, IDatabaseRepository databaseRepository, IFileRepository fileRepository)
        {
            _historicWeatherDataRepository = historicWeatherDataRepository;
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
        }
        #endregion



        public async Task CreateHistoricWeatherDataAsync(bool createFiles, bool createDB)
        {
            await CreateHistoricWeatherData(createFiles, createDB);
        }




        public async Task CreateCities()
        {
            await CreateCitiesForRepository();
        }


        public async Task CreateLocations()
        {
            await CreateLocationsForRepository();
        }

        public async Task CreateCoordinates()
        {
            await CreateCoordinatesRepository();
        }




        private async Task CreateHistoricWeatherData(bool createFiles, bool createDB)
        {
            try
            {
                await RetreiveProcessWeatherData(_latitude, _longitude, _startDate, _endDate, _coordinatesFilePath);

                List<Task> tasks = new List<Task>();

                if (createFiles)
                {
                    var fileTask = _historicWeatherDataRepository.SaveDataToFileAsync(_saveToStorageDto, _baseDirectory);
                    tasks.Add(fileTask);
                }
                if (createDB)
                {
                    var dbTask = _historicWeatherDataRepository.SaveDataToDatabaseAsync(_saveToStorageDto);
                    tasks.Add(dbTask);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Ready for logging
            }

        }




        private async Task RetreiveProcessWeatherData(string latitude, string longitude, DateTime startDate, DateTime endDate, string coordinatesFilePath)
        {
            try
            {
                string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonData = await response.Content.ReadAsStringAsync();
                HistoricWeatherDataDto originalWeatherDataFromAPI = JsonSerializer.Deserialize<HistoricWeatherDataDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                await ProcessAllCoordinates(originalWeatherDataFromAPI, coordinatesFilePath);
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
        }




        private async Task ProcessAllCoordinates(HistoricWeatherDataDto originalWeatherDataFromAPI, string coordinatesFilePath)
        {
            try
            {
                await foreach (var coordinate in ReadCoordinatesAsync(coordinatesFilePath))
                {
                    string[] parts = coordinate.Split('-');
                   
                        HistoricWeatherDataDto? modifiedData = ModifyData(originalWeatherDataFromAPI);
                        if (modifiedData != null)
                        {
                            SaveToStorageDto saveToFileDto = new SaveToStorageDto
                            {
                                HistoricWeatherData = modifiedData,
                                Latitude = parts[0],
                                Longitude = parts[1]
                            };
                            _saveToStorageDto.Add(saveToFileDto);
                        }
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
        }




        private HistoricWeatherDataDto? ModifyData(HistoricWeatherDataDto? originalData)
        {
            try
            {
                if (originalData == null)
                {
                    return null;
                }
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
            catch (Exception ex)
            {
                // Ready for logging
                return null;
            }
        }




        private async IAsyncEnumerable<string> ReadCoordinatesAsync(string filePath)
        {
            var coordinatesText = await File.ReadAllLinesAsync(filePath);
            if (coordinatesText.Length == 0)
            {
                yield break;
            }
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
            try
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
            catch (Exception ex)
            {
                // Ready for logging
                return coordinate;
            }
        }




        private float AdjustValueRandomly(float originalValue, Random random)
        {
            try
            {
                double percentage = (random.NextDouble() * 0.6 - 0.3);  // Adjust by up to ±30%
                float newValue = (float)Math.Round(originalValue * (1 + percentage), 2, MidpointRounding.AwayFromZero);
                return newValue;
            }
            catch (Exception ex)
            {
                // Ready for logging
                return originalValue;
            }
        }




        private async Task CreateCitiesForRepository()
        {
            try
            {
                string _LocationsCities = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Cities.json");
                string jsonContent = File.ReadAllText(_LocationsCities);
                List<City>? cityModels = JsonSerializer.Deserialize<List<City>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (cityModels == null)
                {
                    return;
                }

                await _historicWeatherDataRepository.SaveCitiesToDBAsync(cityModels);
            }
            catch (Exception ex)
            {
                //ready for logging
            }
        }





        private async Task CreateLocationsForRepository()
        {
            try
            {
                string _LocationsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "LocationsSelected.json");
                string jsonContent = File.ReadAllText(_LocationsFilePath);

                List<LocationDto>? locationModels = JsonSerializer.Deserialize<List<LocationDto>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (locationModels is null)
                {
                    return;
                }

                foreach (var location in locationModels)
                {
                    location.Latitude = FormatCoordinate(location.Latitude);
                    location.Longitude = FormatCoordinate(location.Longitude);
                }
                await _historicWeatherDataRepository.SaveLocationsToDBAsync(locationModels);
            }
            catch (Exception)
            {
                //ready for logging
            }
        }




        private async Task CreateCoordinatesRepository()
        {
            try
            {
                string locationsUniqueCoordinatesSelected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "UniqueCoordinatesSelected.json");
                string jsonContent = File.ReadAllText(locationsUniqueCoordinatesSelected);
                List<string>? UniqueCoordinates = JsonSerializer.Deserialize<List<string>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (UniqueCoordinates == null)
                {
                    return;
                }

                await _historicWeatherDataRepository.SaveCoordinatesToDBAsync(UniqueCoordinates);
            }
            catch (Exception ex)
            {
                //ready for logging
            }
        }
    }
}
