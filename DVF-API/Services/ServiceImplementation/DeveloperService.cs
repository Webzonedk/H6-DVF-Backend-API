using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace DVF_API.Services.ServiceImplementation
{

    /// <summary>
    /// This class is used to create the historic weather data for the given date range and save it to the database and/or files.
    /// It also creates the cities, locations, and coordinates for the repository.
    /// </summary>
    public class DeveloperService : IDeveloperService
    {
        #region fields
        private readonly HttpClient _httpClient = new HttpClient();

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/deletedWeatherData/";

        private string _latitude = "55.3235";
        private string _longitude = "11.9639";

        //Used for The database writing
        private ConcurrentQueue<(DateTime, BinaryWeatherStructDto[])> _databaseWriteQueue = new ConcurrentQueue<(DateTime, BinaryWeatherStructDto[])>();
        private SemaphoreSlim _dbSemaphore = new SemaphoreSlim(3);
        private volatile bool _isDataLoadingComplete = false;
        private Dictionary<long, string> _locationCoordinatesWithId = new Dictionary<long, string>();
        List<DateTime> _allDates = new List<DateTime>();
        //---------------------------

        private readonly IHistoricWeatherDataRepository _historicWeatherDataRepository;
        private readonly IUtilityManager _utilityManager;
        private readonly ICrudDatabaseRepository _databaseRepository;
        private readonly ICrudFileRepository _fileRepository;
        private readonly ILocationRepository _locationRepository;
        #endregion




        #region Constructors

        public DeveloperService(IHistoricWeatherDataRepository historicWeatherDataRepository,
            IUtilityManager utilityManager,
            ICrudDatabaseRepository databaseRepository,
            ICrudFileRepository fileRepository,
            ILocationRepository locationRepository)
        {
            _historicWeatherDataRepository = historicWeatherDataRepository;
            _utilityManager = utilityManager;
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
            _locationRepository = locationRepository;
        }
        #endregion




        /// <summary>
        /// Calls the method to create the historic weather data for the given date range and calling the repository to save the data to the database and/or files
        /// </summary>
        /// <param name="password"></param>
        /// <param name="clientIp"></param>
        /// <param name="createFiles"></param>
        /// <param name="createDB"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>Returns a task</returns>
        public async Task CreateHistoricWeatherDataAsync(string password, string clientIp, bool createFiles, bool createDB, DateTime startDate, DateTime endDate)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateHistoricWeatherData(createFiles, createDB, startDate, endDate);
            }
        }




        /// <summary>
        /// Calls the method to create the cities for the repository
        /// </summary>
        /// <param name="password"></param>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        public async Task CreateCities(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateCitiesForRepository();
            }
        }




        /// <summary>
        /// Calls the method to create the locations for the repository
        /// </summary>
        /// <param name="password"></param>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        public async Task CreateLocations(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateLocationsForRepository();
            }
        }




        /// <summary>
        /// Calls the method to create the coordinates for the repository
        /// </summary>
        /// <param name="password"></param>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        public async Task CreateCoordinates(string password, string clientIp)
        {
            if (_utilityManager.Authenticate(password, clientIp))
            {
                await CreateCoordinatesRepository();
            }
        }




        /// <summary>
        /// Creates the historic weather data for the given date range and saves it to the database and/or files
        /// </summary>
        /// <param name="createFiles"></param>
        /// <param name="createDB"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>A task</returns>
        private async Task CreateHistoricWeatherData(bool createFiles, bool createDB, DateTime startDate, DateTime endDate)
        {
            try
            {
                _locationCoordinatesWithId = await _locationRepository.FetchLocationCoordinates(0, 2147483647);
                _allDates = GetAllDates(startDate, endDate);
                Task dbWorker = Task.Run(() => ProcessDatabaseQueue()); // Start the database worker
                if (_locationCoordinatesWithId.Count == 0 || _allDates.Count == 0)
                {
                    return;
                }

                foreach (var date in _allDates)
                {
                    BinaryWeatherStructDto[]? weatherstructDtoArray = await RetreiveProcessWeatherData(_latitude, _longitude, date, _locationCoordinatesWithId);
                    if (weatherstructDtoArray == null || weatherstructDtoArray.Length == 0)
                    {
                        continue; // Skip if no data to process
                    }

                    if (createFiles)
                    {
                        await SaveDataToFilesAsync(date, weatherstructDtoArray); // File saving remains asynchronous but is awaited
                    }

                    if (createDB)
                    {
                        EnqueueDataForDatabase(date, weatherstructDtoArray); // Enqueue data for database writing
                    }

                weatherstructDtoArray = null; // Clear the array to free up memory
                }
                _locationCoordinatesWithId.Clear(); // Clear the dictionary to free up memory
                _allDates.Clear(); // Clear the list to free up memory
                _isDataLoadingComplete = true;
                await dbWorker; // Ensure the database worker completes before finishing
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in CreateHistoricWeatherData: " + ex.Message);
                throw;
            }
        }




        /// <summary>
        /// Enqueues the data for database writing to the queue to ensure that only one write operation happens at a time and that the data is saved in chunks to avoid server timeouts
        /// </summary>
        /// <param name="date"></param>
        /// <param name="weatherDataArray"></param>
        /// <param name="chunkSize"></param>
        private void EnqueueDataForDatabase(DateTime date, BinaryWeatherStructDto[] weatherDataArray, int chunkSize = 500000)
        {
            for (int i = 0; i < weatherDataArray.Length; i += chunkSize)
            {
                BinaryWeatherStructDto[] chunk = weatherDataArray.Skip(i).Take(chunkSize).ToArray();
                _databaseWriteQueue.Enqueue((date, chunk));
            }
        }




        /// <summary>
        /// Processes the database queue and calls the repository to save the data to the database
        /// </summary>
        /// <returns>A task</returns>
        private async Task ProcessDatabaseQueue()
        {
            while (!_isDataLoadingComplete || !_databaseWriteQueue.IsEmpty) // Continues checking the queue
            {
                if (_databaseWriteQueue.TryDequeue(out var item))
                {
                    bool isDoneSavingToDb;
                    await _dbSemaphore.WaitAsync();
                    try
                    {
                        do
                        {
                            isDoneSavingToDb = await _historicWeatherDataRepository.SaveDataToDatabaseAsync(item.Item1, item.Item2);
                            if (!isDoneSavingToDb)
                            {
                                await Task.Delay(30000); // Wait 30 sec before trying again if not successful
                            }
                        } while (!isDoneSavingToDb); // Repeat until data is successfully saved
                    }
                    finally
                    {
                        _dbSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(1000); // Wait for a moment before trying again if the queue is temporarily empty
                }
            }
            _databaseWriteQueue.Clear(); // Clear the queue to free up memory
        }




        /// <summary>
        /// Calls the repository to save the data to files
        /// </summary>
        /// <param name="date"></param>
        /// <param name="weatherDataArray"></param>
        /// <returns>A task</returns>
        private async Task SaveDataToFilesAsync(DateTime date, BinaryWeatherStructDto[] weatherDataArray)
        {
            var year = date.Year.ToString();
            var monthAndDate = date.ToString("MMdd", CultureInfo.InvariantCulture);
            var yearDirectory = Path.Combine(_baseDirectory, year);
            Directory.CreateDirectory(yearDirectory);
            var fileName = Path.Combine(yearDirectory, $"{monthAndDate}.bin");
            await _historicWeatherDataRepository.SaveDataToFileAsync(fileName, weatherDataArray);
        }




        /// <summary>
        /// Retrieves and processes the weather data for the given date for all locations
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="date"></param>
        /// <param name="locationCoordinatesWithId"></param>
        /// <returns>An array of WeatherStruct</returns>
        private async Task<BinaryWeatherStructDto[]> RetreiveProcessWeatherData(string latitude, string longitude, DateTime date, Dictionary<long, string> locationCoordinatesWithId)
        {
            try
            {
                string url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={date:yyyy-MM-dd}&end_date={date:yyyy-MM-dd}&hourly=temperature_2m,relative_humidity_2m,rain,wind_speed_10m,wind_direction_10m,wind_gusts_10m,global_tilted_irradiance_instant&wind_speed_unit=ms";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonData = await response.Content.ReadAsStringAsync();
                HistoricWeatherDataDto? originalWeatherDataFromAPI = JsonSerializer.Deserialize<HistoricWeatherDataDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (originalWeatherDataFromAPI != null)
                {
                    BinaryWeatherStructDto[] weatherDataForSelectedDate = await ProcessAllCoordinates(originalWeatherDataFromAPI, locationCoordinatesWithId);
                    return weatherDataForSelectedDate;
                }
                else
                {
                    return Array.Empty<BinaryWeatherStructDto>();
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
                return Array.Empty<BinaryWeatherStructDto>();
            }
        }




        /// <summary>
        /// Creates a list of all dates between the start and end date
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>A list of DateTime objects</returns>
        private List<DateTime> GetAllDates(DateTime startDate, DateTime endDate)
        {
            List<DateTime> dateList = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateList.Add(date);
            }

            return dateList;
        }




        /// <summary>
        /// Processes all coordinates and creates a list of WeatherStruct based on the HistoricWeatherDataDto but with adjusted values and the LocationId to simulate real data
        /// </summary>
        /// <param name="originalWeatherDataFromAPI"></param>
        /// <param name="locationCoordinatesWithId"></param>
        /// <returns></returns>
        private async Task<BinaryWeatherStructDto[]> ProcessAllCoordinates(HistoricWeatherDataDto originalWeatherDataFromAPI, Dictionary<long, string> locationCoordinatesWithId)
        {
            int degreeOfParallelism = _utilityManager.CalculateOptimalDegreeOfParallelism();
            SemaphoreSlim semaphore = new SemaphoreSlim(degreeOfParallelism);

            ConcurrentBag<BinaryWeatherStructDto> weatherStructs = new ConcurrentBag<BinaryWeatherStructDto>();

            var tasks = locationCoordinatesWithId.Select(async kvp =>
            {
                await semaphore.WaitAsync();
                try
                {
                    BinaryWeatherStructDto[] modifiedData = ConvertToCreateDto(originalWeatherDataFromAPI, kvp.Key);
                    foreach (var item in modifiedData)
                    {
                        weatherStructs.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in ProcessAllCoordinates: {ex.Message}");
                    // Ready for logging
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return SortWeatherData(weatherStructs.ToArray());
        }

        private unsafe BinaryWeatherStructDto[] SortWeatherData(BinaryWeatherStructDto[] weatherData)
        {
            return weatherData.OrderBy(weatherStruct => weatherStruct.LocationId)
                              .ThenBy(weatherStruct => *((float*)weatherStruct.WeatherData))
                              .ToArray();
        }




        /// <summary>
        /// Creates a list of WeatherStruct based on the HistoricWeatherDataDto but with adjusted values and the LocationId to simulate real data
        /// </summary>
        /// <param name="historicData"></param>
        /// <param name="LocationId"></param>
        /// <returns>An array of WeatherStruct</returns>
        private unsafe BinaryWeatherStructDto[] ConvertToCreateDto(HistoricWeatherDataDto historicData, long LocationId)
        {
            BinaryWeatherStructDto[] weatherDataList = new BinaryWeatherStructDto[historicData.Hourly.Time.Length];

            for (int i = 0; i < historicData.Hourly.Time.Length; i++)
            {
                var random = new Random();
                // string dateTimeString = historicData.Hourly.Time[i].Replace('T', ' ').Insert(historicData.Hourly.Time[i].IndexOf('T'),"HH");
                DateTime dateTime = DateTime.ParseExact(historicData.Hourly.Time[i], "yyyy-MM-ddTHH:mm", null);
                float timeAsFloat = ConvertToFloatTime(dateTime.Hour, dateTime.Minute);

                fixed (float* weatherDataPtr = weatherDataList[i].WeatherData)
                {
                    weatherDataPtr[0] = timeAsFloat;
                    weatherDataPtr[1] = AdjustValueRandomly(historicData.Hourly.Temperature[i], random);
                    weatherDataPtr[2] = AdjustValueRandomly(historicData.Hourly.RelativeHumidity[i], random);
                    weatherDataPtr[3] = AdjustValueRandomly(historicData.Hourly.Rain[i], random);
                    weatherDataPtr[4] = AdjustValueRandomly(historicData.Hourly.WindSpeed[i], random);
                    weatherDataPtr[5] = AdjustValueRandomly(historicData.Hourly.WindDirection[i], random);
                    weatherDataPtr[6] = AdjustValueRandomly(historicData.Hourly.WindGusts[i], random);
                    weatherDataPtr[7] = AdjustValueRandomly(historicData.Hourly.GlobalTiltedIrRadianceInstant[i], random);
                }
                weatherDataList[i].LocationId = LocationId;
            }
            return weatherDataList;
        }




        /// <summary>
        /// Converts the time to a float value
        /// </summary>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        /// <returns>A float value for the time</returns>
        private float ConvertToFloatTime(int hours, int minutes)
        {
            float timeAsFloat = hours + (minutes / 60.0f);
            return timeAsFloat;
        }





        /// <summary>
        /// Formats the coordinate to a specific format with 2 digits before the decimal point and 8 digits after the decimal point
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns>A string with the formatted coordinate</returns>
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




        /// <summary>
        /// Adjusts the value randomly by up to ±30% for the given value
        /// </summary>
        /// <param name="originalValue"></param>
        /// <param name="random"></param>
        /// <returns>A float value for the adjusted value with a maximum of 2 decimal places</returns>
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




        /// <summary>
        /// Create cities for repository based on Cities.json file. Calling the repository to save the cities
        /// </summary>
        /// <returns>A task</returns>
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




        /// <summary>
        /// Create locations for repository based on LocationsSelected.json file Calling the repository to save the locations
        /// </summary>
        /// <returns>A task</returns>
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




        /// <summary>
        /// Create coordinates for repository based on UniqueCoordinatesSelected.json file. Calling the repository to save the coordinates
        /// </summary>
        /// <returns>A task</returns>
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
