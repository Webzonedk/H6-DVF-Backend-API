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
        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/app/data/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/app/data/deletedWeatherData/";

        //private string _coordinatesFilePath = "..\\DVF-API\\Sources\\UniqueCoordinatesSelected.json";
        private string _coordinatesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "UniqueCoordinatesSelected.json");
        private string _latitude = "55.3235";
        private string _longitude = "11.9639";
        private DateTime _startDate = new DateTime(2024, 03, 30);
        private DateTime _endDate = new DateTime(2024, 04, 01);
        private HistoricWeatherDataDto? _originalWeatherData;
        private List<SaveToFileDto> _saveToFileDtoList = new List<SaveToFileDto>();


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




        private async Task CreateHistoricWeatherData(bool createFiles, bool createDB)
        {
            try
            {
                await GenerateAndSaveOriginalData(createFiles, createDB, _latitude, _longitude, _startDate, _endDate, _baseDirectory);
                await ProcessAllCoordinates(createFiles, createDB, _coordinatesFilePath);
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
            finally
            {
                if (createFiles)
                {
                    await _historicWeatherDataRepository.SaveDataToFileAsync(_saveToFileDtoList, _baseDirectory);
                }
                if (createDB)
                {
                    await _historicWeatherDataRepository.SaveDataToDatabaseAsync(_saveToFileDtoList);
                }
            }
        }




        private async Task GenerateAndSaveOriginalData(bool createFiles, bool createDB, string latitude, string longitude, DateTime startDate, DateTime endDate, string baseFolder)
        {
            try
            {
                string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonData = await response.Content.ReadAsStringAsync();
                _originalWeatherData = JsonSerializer.Deserialize<HistoricWeatherDataDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (_originalWeatherData != null)
                {
                    var formatedLatitude = FormatCoordinate(latitude);
                    var formatedLongitude = FormatCoordinate(longitude);
                    SaveToFileDto saveToFileDto = new SaveToFileDto
                    {
                        HistoricWeatherData = _originalWeatherData,
                        Latitude = formatedLatitude,
                        Longitude = formatedLongitude
                    };
                    _saveToFileDtoList.Add(saveToFileDto);
                    //if (createFiles)
                    //{
                    //    await _historicWeatherDataRepository.SaveDataToFileAsync(_originalWeatherData, formatedLatitude, formatedLongitude, baseFolder);
                    //}
                    //if (createDB)
                    //{
                    //    await _historicWeatherDataRepository.SaveDataToDatabaseAsync(_originalWeatherData, formatedLatitude, formatedLongitude);
                    //}
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




        public async Task ProcessAllCoordinates(bool createFiles, bool createDB, string coordinatesFilePath)
        {
            try
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
                            SaveToFileDto saveToFileDto = new SaveToFileDto
                            {
                                HistoricWeatherData = modifiedData,
                                Latitude = parts[0],
                                Longitude = parts[1]
                            };
                            _saveToFileDtoList.Add(saveToFileDto);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
        }




        private IEnumerable<string> ReadCoordinates(string filePath)
        {
            var coordinatesText = File.ReadAllLines(filePath);
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

                await _historicWeatherDataRepository.InsertCitiesToDB(cityModels);
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
                await _historicWeatherDataRepository.InsertLocationsToDB(locationModels);
            }
            catch (Exception)
            {
                //ready for logging
            }
        }
    }
}
