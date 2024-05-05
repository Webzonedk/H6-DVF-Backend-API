using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Diagnostics;
using System.Globalization;

namespace DVF_API.Services.ServiceImplementation
{

    /// <summary>
    /// This class is responsible for handling the data retrieval and processing of weather data.
    /// It retrieves weather data from either the database or files, and adds sun angles to the data.
    /// </summary>
    public class DataService : IDataService
    {

        #region Fields
        private readonly ICrudDatabaseRepository _crudDatabaseRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICrudFileRepository _crudFileRepository;
        private readonly ISolarPositionManager _solarPositionManager;
        private readonly IUtilityManager _utilityManager;

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        #endregion




        #region Constructor
        public DataService(
            ICrudDatabaseRepository crudDatabaseRepository, ILocationRepository locationRepository,
            ISolarPositionManager solarPositionManager, IUtilityManager utilityManager,
            ICrudFileRepository crudFileRepository)
        {
            _crudDatabaseRepository = crudDatabaseRepository;
            _locationRepository = locationRepository;
            _crudFileRepository = crudFileRepository;
            _solarPositionManager = solarPositionManager;
            _utilityManager = utilityManager;
        }
        #endregion




        /// <summary>
        /// method returns a list of addresses matching a partial inputted address
        /// </summary>
        /// <param name="partialAddress"></param>
        /// <returns></returns>
        public async Task<List<string>> GetAddressesFromDBMatchingInputs(string partialAddress)
        {
            return await _locationRepository.FetchMatchingAddresses(partialAddress);
        }




        /// <summary>
        /// method returns the number of total locations in database
        /// </summary>
        /// <returns></returns>
        public Task<int> CountLocations()
        {
            return _locationRepository.FetchLocationCount();
        }




        /// <summary>
        /// method returns a number of coordinates based on a range of location id´s from database
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns></returns>
        public async Task<Dictionary<long, string>> GetLocationCoordinates(int fromIndex, int toIndex)
        {
            return await _locationRepository.FetchLocationCoordinates(fromIndex, toIndex);
        }




        /// <summary>
        /// Get weather data from file or database, adding sun angles to the data. making it ready for the front end.
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns>Returns a MetaDataDto object containing the weather data and sun angles.</returns>
        public async Task<MetaDataDto> GetWeatherDataService(SearchDto searchDto)
        {
            MetaDataDto metaDataDto = new MetaDataDto();
            if (searchDto.ToggleDB)
            {
                return await RetrieveDataFromDatabase(searchDto);
            }

            List<DateOnly> dateList = Enumerable.Range(0, 1 + searchDto.ToDate.DayNumber - searchDto.FromDate.DayNumber)
                          .Select(offset => searchDto.FromDate.AddDays(offset))
                          .ToList();

            List<BinaryDataFromFileDto> listOfBinaryDataFromFileDto = await _locationRepository.FetchAddressByCoordinates(searchDto);
            List<WeatherDataDto> weatherDataDtoList = new List<WeatherDataDto>();

            double dataRetrievalCpuUsageTotal = 0;
            double dataRetrievalMemoryTotal = 0;
            double dataProcessingCpuUsageTotal = 0;
            double dataProcessingMemoryTotal = 0;

            try
            {
                for (int i = 0; i < dateList.Count; i++)
                {

                    (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPUTime();
                    double startMemory = _utilityManager.BeginMeasureMemory();

                    double yearDate = _utilityManager.ConvertDateTimeToDouble(dateList[i].ToString());
                    var fullDate = _utilityManager.MixedYearDateTimeSplitter(yearDate)[0].ToString();
                    var year = fullDate.Substring(0, 4);
                    var monthDay = fullDate.Substring(4, 4);
                    var directory = Path.Combine(_baseDirectory, year);
                    var fileName = Path.Combine(directory, $"{monthDay}.bin");

                    BinaryWeatherStructDto[] binaryWeatherStructWithWeatherData = await LoadDataFromFiles(listOfBinaryDataFromFileDto, dateList[i], fileName);

                    var retrievalCpuResult = _utilityManager.StopMeasureCPUTime(cpuTimeBefore, stopwatch);
                    double retrievalMemoryUsed = _utilityManager.StopMeasureMemory(startMemory);

                    dataRetrievalCpuUsageTotal += retrievalCpuResult.CpuUsagePercentage;
                    dataRetrievalMemoryTotal += retrievalMemoryUsed;

                    (cpuTimeBefore, stopwatch) = _utilityManager.BeginMeasureCPUTime();
                    startMemory = _utilityManager.BeginMeasureMemory();

                    Dictionary<long, LocationDto> locations = await GetLocations(metaDataDto, searchDto);
                    ProcessWeatherData(weatherDataDtoList, binaryWeatherStructWithWeatherData, locations, fileName);

                    var processingCpuResult = _utilityManager.StopMeasureCPUTime(cpuTimeBefore, stopwatch);
                    double processingMemoryUsed = _utilityManager.StopMeasureMemory(startMemory);

                    dataProcessingCpuUsageTotal += processingCpuResult.CpuUsagePercentage;
                    dataProcessingMemoryTotal += processingMemoryUsed;
                }
            }
            catch (Exception ex)
            {
                metaDataDto.ResponseMessage = $"En fejl opstod under datahentning: {ex.Message}";
            }

            metaDataDto.WeatherData = weatherDataDtoList;

            int metaDataModelInBytes = _utilityManager.GetModelSize(metaDataDto);
            metaDataDto.DataLoadedMB = _utilityManager.ConvertBytesToFormat(metaDataModelInBytes);
            metaDataDto.RamUsage = _utilityManager.ConvertBytesToFormat(dataRetrievalMemoryTotal);
            metaDataDto.CpuUsage = (float)Math.Round(dataRetrievalCpuUsageTotal, 2, MidpointRounding.AwayFromZero);

            metaDataDto.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(dataRetrievalCpuUsageTotal);
            metaDataDto.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(dataProcessingCpuUsageTotal);
            metaDataDto.ConvertionRamUsage = _utilityManager.ConvertBytesToFormat(dataProcessingMemoryTotal);
            metaDataDto.ConvertionCpuUsage = (float)Math.Round(dataProcessingCpuUsageTotal, 2, MidpointRounding.AwayFromZero);

            return metaDataDto;
        }




        /// <summary>
        /// Loads data from files and returns a BinaryWeatherStructDto array.
        /// </summary>
        /// <param name="listOfBinaryDataFromFileDto"></param>
        /// <param name="date"></param>
        /// <param name="fileName"></param>
        /// <returns>A BinaryWeatherStructDto array containing weather data with shared memory.</returns>
        private async Task<BinaryWeatherStructDto[]> LoadDataFromFiles(List<BinaryDataFromFileDto> listOfBinaryDataFromFileDto, DateOnly date, string fileName)
        {
            BinarySearchInFilesDto binarySearchInFilesDto = new BinarySearchInFilesDto();
            var firstLocationId = listOfBinaryDataFromFileDto.Select(x => (x.LocationId)).First();
            var lastLocationId = listOfBinaryDataFromFileDto.Select(x => (x.LocationId)).Last();

            if (listOfBinaryDataFromFileDto.Count == 1)
            {
                binarySearchInFilesDto.FromByte = (listOfBinaryDataFromFileDto[0].LocationId - 1) * 960;
                binarySearchInFilesDto.ToByte = listOfBinaryDataFromFileDto[0].LocationId * 960 - 1;
                binarySearchInFilesDto.FilePath = fileName;
            }
            else
            {
                binarySearchInFilesDto.FromByte = (firstLocationId - 1) * 960;
                binarySearchInFilesDto.ToByte = lastLocationId * 960 - 1;
                binarySearchInFilesDto.FilePath = fileName;
            }
            return await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDto);
        }




        /// <summary>
        /// Processes weather data and adds it to a list of WeatherDataDto objects. This method is called for each file. and adds sun angles to the data.
        /// It also adds the data to a list of WeatherDataDto objects. and contains unsafe code to handle the shared memory.
        /// </summary>
        /// <param name="weatherDataDtoList"></param>
        /// <param name="returnedStructArrayWithWeatherData"></param>
        /// <param name="locations"></param>
        /// <param name="fileName"></param>
        private void ProcessWeatherData(List<WeatherDataDto> weatherDataDtoList, BinaryWeatherStructDto[] returnedStructArrayWithWeatherData, Dictionary<long, LocationDto> locations, string fileName)
        {
            long Id = 0;
            float time = 0;
            string tempDate = "";
            string? tempYear = "";

            foreach (var weatherStruct in returnedStructArrayWithWeatherData)
            {
                WeatherDataDto historicWeatherDataToFileDto = new WeatherDataDto();

                unsafe
                {
                    Id = weatherStruct.LocationId;
                    time = weatherStruct.WeatherData[0];
                    tempDate = Path.GetFileNameWithoutExtension(fileName);
                    tempYear = Path.GetFileName(Path.GetDirectoryName(fileName));

                    historicWeatherDataToFileDto.DateAndTime = DateTime.ParseExact(string.Concat(tempYear, tempDate, time.ToString("0000")), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
                    historicWeatherDataToFileDto.Address = $"{locations[Id].StreetName} {locations[Id].StreetNumber}, {locations[Id].PostalCode} {locations[Id].CityName}";
                    historicWeatherDataToFileDto.Latitude = locations[Id].Latitude;
                    historicWeatherDataToFileDto.Longitude = locations[Id].Longitude;
                    historicWeatherDataToFileDto.TemperatureC = weatherStruct.WeatherData[1];
                    historicWeatherDataToFileDto.RelativeHumidity = weatherStruct.WeatherData[2];
                    historicWeatherDataToFileDto.Rain = weatherStruct.WeatherData[3];
                    historicWeatherDataToFileDto.WindSpeed = weatherStruct.WeatherData[4];
                    historicWeatherDataToFileDto.WindDirection = weatherStruct.WeatherData[5];
                    historicWeatherDataToFileDto.WindGust = weatherStruct.WeatherData[6];
                    historicWeatherDataToFileDto.GlobalTiltedIrRadiance = weatherStruct.WeatherData[7];
                    var sunResult = _solarPositionManager.CalculateSunAngles(historicWeatherDataToFileDto.DateAndTime, double.Parse(historicWeatherDataToFileDto.Latitude, CultureInfo.InvariantCulture), double.Parse(historicWeatherDataToFileDto.Longitude, CultureInfo.InvariantCulture));
                    historicWeatherDataToFileDto.SunAzimuthAngle = (float)sunResult.SunAzimuth;
                    historicWeatherDataToFileDto.SunElevationAngle = (float)sunResult.SunAltitude;
                }

                weatherDataDtoList.Add(historicWeatherDataToFileDto);
            }
        }




        /// <summary>
        /// Retrieves locations from the database based on the searchDto. If the searchDto contains coordinates, it will return the location based on the coordinates.
        /// </summary>
        /// <param name="metaDataDto"></param>
        /// <param name="searchDto"></param>
        /// <returns>A dictionary containing location id´s and locationDto´s.</returns>
        private async Task<Dictionary<long, LocationDto>> GetLocations(MetaDataDto metaDataDto, SearchDto searchDto)
        {
            Dictionary<long, LocationDto> locations = new Dictionary<long, LocationDto>();
            try
            {
                if (searchDto.Coordinates.Count != 1)
                {
                    locations = await _locationRepository.GetAllLocationCoordinates();
                }
                else
                {
                    var location = await _locationRepository.FetchAddressByCoordinates(searchDto);
                    int locationId = location[0].LocationId;
                    var coordinateDictionary = await _locationRepository.FetchLocationCoordinates(locationId, locationId);
                    var address = location[0].Address.Split(' ');
                    var coordinates = coordinateDictionary[locationId].Split("-");
                    LocationDto locationDto = new LocationDto()
                    {
                        Latitude = coordinates[0],
                        Longitude = coordinates[1],
                        StreetName = address[0],
                        StreetNumber = address[1],
                        PostalCode = address[2],
                        CityName = address[3]
                    };
                    locations.Add(locationId, locationDto);
                }
            }
            catch (Exception e)
            {
                metaDataDto.ResponseMessage = $"En fejl opstod under datahentning i locations dictionary: {e.Message}";
            }
            return locations;
        }




        /// <summary>
        /// Retrieves weather data from the database and adds sun angles to the data. making it ready for the front end.
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns>A MetaDataDto object containing the weather data and sun angles.</returns>
        private async Task<MetaDataDto> RetrieveDataFromDatabase(SearchDto searchDto)
        {
            (TimeSpan cpuTimeBeforeRetrievingFromDatabase, Stopwatch fileStopwatch) = _utilityManager.BeginMeasureCPUTime();
            double StartMemoryDatabase = _utilityManager.BeginMeasureMemory();

            MetaDataDto metaDataDto = await _crudDatabaseRepository.FetchWeatherDataAsync(searchDto);

            var (cpuUsageDatabase, elapsedTimeFile) = _utilityManager.StopMeasureCPUTime(cpuTimeBeforeRetrievingFromDatabase, fileStopwatch);
            double memoryUsedDatabase = _utilityManager.StopMeasureMemory(StartMemoryDatabase);

            if (metaDataDto != null)
            {
                (TimeSpan cpuTimeBeforeDataConversion, Stopwatch DataConversionStopwatch) = _utilityManager.BeginMeasureCPUTime();
                double DataConvertionStartMemory = _utilityManager.BeginMeasureMemory();

                List<Task> tasks = new List<Task>();
                for (int i = 0; i < metaDataDto.WeatherData.Count; i++)
                {
                    int index = i;
                    tasks.Add(Task.Run(() =>
                    {
                        var result = _solarPositionManager.CalculateSunAngles(metaDataDto.WeatherData[index].DateAndTime, double.Parse(metaDataDto.WeatherData[index].Latitude, CultureInfo.InvariantCulture), double.Parse(metaDataDto.WeatherData[index].Longitude, CultureInfo.InvariantCulture));
                        metaDataDto.WeatherData[index].SunAzimuthAngle = (float)result.SunAzimuth;
                        metaDataDto.WeatherData[index].SunElevationAngle = (float)result.SunAltitude;
                    }));
                }
                await Task.WhenAll(tasks);

                var (cpuUsageDataConversion, elapsedTimeDataConversion) = _utilityManager.StopMeasureCPUTime(cpuTimeBeforeDataConversion, DataConversionStopwatch);
                double memoryUsedDataConversion = _utilityManager.StopMeasureMemory(DataConvertionStartMemory);

                double totalBytes = _utilityManager.GetModelSize(metaDataDto);
                string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);

                metaDataDto.DataLoadedMB = dataCollectedInMB;
                metaDataDto.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(elapsedTimeFile);
                metaDataDto.RamUsage = _utilityManager.ConvertBytesToFormat(memoryUsedDatabase);
                metaDataDto.CpuUsage = (float)Math.Round(cpuUsageDatabase, 2, MidpointRounding.AwayFromZero);

                metaDataDto.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(elapsedTimeDataConversion);
                metaDataDto.ConvertionRamUsage = _utilityManager.ConvertBytesToFormat(memoryUsedDataConversion);
                metaDataDto.ConvertionCpuUsage = (float)Math.Round(cpuUsageDataConversion, 2, MidpointRounding.AwayFromZero);
            }
            return metaDataDto!;
        }
    }
}
