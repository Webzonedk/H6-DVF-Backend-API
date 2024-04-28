using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        private string _latitude = "55.3235";
        private string _longitude = "11.9639";
        private DateTime startDate = new DateTime(2024, 04, 01);
        private DateTime endDate = new DateTime(2024, 04, 01);


        private readonly IHistoricWeatherDataRepository _historicWeatherDataRepository;
        private readonly IUtilityManager _utilityManager;
        private readonly ICrudDatabaseRepository _databaseRepository;
        private readonly ICrudFileRepository _fileRepository;
        private readonly ILocationRepository _locationRepository;
        #endregion




        #region Constructors

        public DeveloperService(IHistoricWeatherDataRepository historicWeatherDataRepository, IUtilityManager utilityManager, ICrudDatabaseRepository databaseRepository, ICrudFileRepository fileRepository, ILocationRepository locationRepository)
        {
            _historicWeatherDataRepository = historicWeatherDataRepository;
            _utilityManager = utilityManager;
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
            _locationRepository = locationRepository;
        }
        #endregion




        public async Task CreateHistoricWeatherDataAsync(string password, string clientIp, bool createFiles, bool createDB, DateTime startDate, DateTime endDate)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateHistoricWeatherData(createFiles, createDB, startDate, endDate);
            }
        }




        public async Task CreateCities(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateCitiesForRepository();
            }
        }




        public async Task CreateLocations(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateLocationsForRepository();
            }
        }




        public async Task CreateCoordinates(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateCoordinatesRepository();
            }
        }




        private async Task CreateHistoricWeatherData(bool createFiles, bool createDB, DateTime startDate, DateTime endDate)
        {
            try
            {
                List<SaveToStorageDto> saveToStorageDtos = new List<SaveToStorageDto>();

                Dictionary<int, string> locationCoordinatesWithId = await _locationRepository.FetchLocationCoordinates(0, 2147483647); // ca 1,2 seconds
                await RetreiveProcessWeatherData(saveToStorageDtos, _latitude, _longitude, startDate, endDate, locationCoordinatesWithId);// ca. 2,9 seconds

                if (saveToStorageDtos == null)
                {
                    return;
                }
                if (createDB)
                {
                    //await _historicWeatherDataRepository.SaveDataToDatabaseAsync(saveToStorageDtos);

                }
                if (createFiles)
                {
                    try
                    {
                    await CreateWeatherDataAndSendItToRepository(saveToStorageDtos);
                    }
                    finally
                    {
                    saveToStorageDtos.Clear();
                    _utilityManager.CleanUpRessources();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
            }

        }




        private async Task CreateWeatherDataAndSendItToRepository(List<SaveToStorageDto> saveToStorageDtos)
        {
            List<HistoricWeatherDataToFileDto> historicWeatherDataToFileDtos = new List<HistoricWeatherDataToFileDto>();

            List<Task> tasks = new List<Task>();
            object lockObject = new object();
            SemaphoreSlim semaphore = new SemaphoreSlim(_utilityManager.CalculateOptimalDegreeOfParallelism());

            foreach (var data in saveToStorageDtos)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        for (int index = 0; index < data.HistoricWeatherData.Hourly.Time.Length; index++)
                        {
                            var historicWeatherDataToFileDto = new HistoricWeatherDataToFileDto
                            {
                                Id = data.LocationId,
                                Time = _utilityManager.ConvertDateTimeToFloatInternal(data.HistoricWeatherData.Hourly.Time[index]),
                                Temperature_2m = data.HistoricWeatherData.Hourly.Temperature_2m[index],
                                Relative_Humidity_2m = data.HistoricWeatherData.Hourly.Relative_Humidity_2m[index],
                                Rain = data.HistoricWeatherData.Hourly.Rain[index],
                                Wind_Speed_10m = data.HistoricWeatherData.Hourly.Wind_Speed_10m[index],
                                Wind_Direction_10m = data.HistoricWeatherData.Hourly.Wind_Direction_10m[index],
                                Wind_Gusts_10m = data.HistoricWeatherData.Hourly.Wind_Gusts_10m[index],
                                Global_Tilted_Irradiance_Instant = data.HistoricWeatherData.Hourly.Global_Tilted_Irradiance_Instant[index]
                            };

                            lock (lockObject)
                            {
                                historicWeatherDataToFileDtos.Add(historicWeatherDataToFileDto);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"{ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();

            ConcurrentDictionary<double, (string, float)> timeSplitCache = new ConcurrentDictionary<double, (string, float)>();

            var groupedData = historicWeatherDataToFileDtos
                              .AsParallel()
                              .WithDegreeOfParallelism(Environment.ProcessorCount - 4)
                              .GroupBy(dto =>
                              {
                                  var key = timeSplitCache.GetOrAdd(dto.Time, time =>
                                  {
                                      var split = _utilityManager.MixedYearDateTimeSplitter(time);
                                      return (split[0].ToString(), (float)split[1]);
                                  });
                                  return key.Item1;
                              })
                              .ToList();

            historicWeatherDataToFileDtos.Clear();

            try
            {
                tasks = new List<Task>();
                lockObject = new object();
                semaphore = new SemaphoreSlim(_utilityManager.CalculateOptimalDegreeOfParallelism());
                foreach (var group in groupedData)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var orderedList = group
                                              .OrderBy(x => x.Id)
                                              .ThenBy(x =>
                                              {
                                                  var hourMinute = timeSplitCache.GetOrAdd(x.Time, time =>
                                                  {
                                                      var splitResults = _utilityManager.MixedYearDateTimeSplitter(time);
                                                      return (splitResults[0].ToString(), (float)splitResults[1]);
                                                  });
                                                  return hourMinute.Item2;
                                              })
                                              .ToList();

                            byte[] byteArrayToSaveToFile = ConvertModelToBytesArray(orderedList);
                            orderedList.Clear();

                            string date = _utilityManager.MixedYearDateTimeSplitter(group.First().Time)[0].ToString()!; // Full date YYYYMMDD
                            var year = date.Substring(0, 4);
                            var monthDay = date.Substring(4, 4);
                            var yearDirectory = Path.Combine(_baseDirectory, year);
                            Directory.CreateDirectory(yearDirectory);
                            var fileName = Path.Combine(yearDirectory, $"{monthDay}.bin");

                            await _historicWeatherDataRepository.SaveDataToFileAsync(fileName, byteArrayToSaveToFile);
                            byteArrayToSaveToFile = Array.Empty<byte>();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error in converting and saving loop{ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                tasks.Clear();
                timeSplitCache.Clear();
                groupedData.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
            }

        }




        private byte[] ConvertModelToBytesArray(List<HistoricWeatherDataToFileDto> orderedList)
        {

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    try
                    {
                        foreach (var groupItem in orderedList)
                        {
                            binaryWriter.Write(groupItem.Id);
                            binaryWriter.Write((float)_utilityManager.MixedYearDateTimeSplitter(groupItem.Time)[1]);
                            binaryWriter.Write(groupItem.Temperature_2m);
                            binaryWriter.Write(groupItem.Relative_Humidity_2m);
                            binaryWriter.Write(groupItem.Rain);
                            binaryWriter.Write(groupItem.Wind_Speed_10m);
                            binaryWriter.Write(groupItem.Wind_Direction_10m);
                            binaryWriter.Write(groupItem.Wind_Gusts_10m);
                            binaryWriter.Write(groupItem.Global_Tilted_Irradiance_Instant);
                        }
                    }
                    catch (Exception)
                    {
                        // Ready for logging
                    }
                }
                return stream.ToArray();
            }
        }





        private async Task RetreiveProcessWeatherData(List<SaveToStorageDto> saveToStorageDtos, string latitude, string longitude, DateTime startDate, DateTime endDate, Dictionary<int, string> locationCoordinatesWithId)
        {
            try
            {
                string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonData = await response.Content.ReadAsStringAsync();
                HistoricWeatherDataDto originalWeatherDataFromAPI = JsonSerializer.Deserialize<HistoricWeatherDataDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                jsonData = null;
                if (originalWeatherDataFromAPI != null)
                {
                    await ProcessAllCoordinates(saveToStorageDtos, originalWeatherDataFromAPI, locationCoordinatesWithId);
                }
                else
                {
                    saveToStorageDtos = new List<SaveToStorageDto>();
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
        }




        private async Task ProcessAllCoordinates(List<SaveToStorageDto> saveToStorageDtos, HistoricWeatherDataDto originalWeatherDataFromAPI, Dictionary<int, string> locationCoordinatesWithId)
        {

            List<Task> tasks = new List<Task>();
            object lockObject = new object();

            SemaphoreSlim semaphore = new SemaphoreSlim(_utilityManager.CalculateOptimalDegreeOfParallelism());

            foreach (KeyValuePair<int, string> keyValuePair in locationCoordinatesWithId)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string[] parts = keyValuePair.Value.Split('-');
                        HistoricWeatherDataDto? modifiedData = ModifyData(originalWeatherDataFromAPI);
                        if (modifiedData != null)
                        {
                            SaveToStorageDto saveToFileDto = new SaveToStorageDto
                            {
                                HistoricWeatherData = modifiedData,
                                Latitude = parts[0],
                                Longitude = parts[1],
                                LocationId = keyValuePair.Key
                            };
                            lock (lockObject)
                            {
                                saveToStorageDtos.Add(saveToFileDto);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
            tasks.Clear(); 
        }




        private HistoricWeatherDataDto? ModifyData(HistoricWeatherDataDto? originalData)
        {
            if (originalData == null)
            {
                return null;
            }
            try
            {
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
