using DVF_API.Data.Interfaces;
using DVF_API.Domain.BusinessLogic;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace DVF_API.Services.ServiceImplementation
{
    public class DataService : IDataService
    {

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";


        private readonly ICrudDatabaseRepository _crudDatabaseRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICrudFileRepository _crudFileRepository;
        private readonly IBinaryConversionManager _binaryConversionManager;
        private readonly ISolarPositionManager _solarPositionManager;
        private readonly IUtilityManager _utilityManager;

        public DataService(
            ICrudDatabaseRepository crudDatabaseRepository, ILocationRepository locationRepository,
            ICrudFileRepository crudFileRepository, IBinaryConversionManager binaryConversionManager,
            ISolarPositionManager solarPositionManager, IUtilityManager utilityManager)
        {
            _crudDatabaseRepository = crudDatabaseRepository;
            _locationRepository = locationRepository;
            _crudFileRepository = crudFileRepository;
            _binaryConversionManager = binaryConversionManager;
            _solarPositionManager = solarPositionManager;
            _utilityManager = utilityManager;
        }




        public async Task<List<string>> GetAddressesFromDBMatchingInputs(string partialAddress)
        {
            return await _locationRepository.FetchMatchingAddresses(partialAddress);
        }




        public Task<int> CountLocations()
        {
            return _locationRepository.FetchLocationCount();
        }




        public async Task<Dictionary<int, string>> GetLocationCoordinates(int fromIndex, int toIndex)
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
                // start measuring CPU usage and Memory before executing the code
                (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPU();
                long currentBytes = _utilityManager.BeginMeasureMemory();



                //get data
                MetaDataDto modelResult = await _crudDatabaseRepository.FetchWeatherDataAsync(searchDto);


                // return recorded CPU usage and memory usage
                var cpuResult = _utilityManager.StopMeasureCPU(cpuTimeBefore, stopwatch);
                var byteMemory = _utilityManager.StopMeasureMemory(currentBytes);
                string measuredRamUsage = _utilityManager.ConvertBytesToFormat(byteMemory);


                //map data to model if model is retrieved successfully
                if (modelResult != null)
                {
                    //calculate sun angles
                    for (int i = 0; i < modelResult.WeatherData.Count; i++)
                    {
                        var Result = _solarPositionManager.CalculateSunAngles(modelResult.WeatherData[i]);
                        modelResult.WeatherData[i] = Result;
                    }

                    //calculate amount of data
                    int weatherDataInBytes = _utilityManager.GetModelSize(modelResult.WeatherData);
                    int metaDataModelInBytes = _utilityManager.GetModelSize(modelResult);
                    int totalBytes = metaDataModelInBytes + weatherDataInBytes;
                    string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);

                    //map measurements to model
                    modelResult.DataLoadedMB = dataCollectedInMB;
                    modelResult.FetchDataTimer = cpuResult.ElapsedTimeMs;
                    modelResult.CpuUsage = cpuResult.CpuUsage;
                    modelResult.RamUsage = measuredRamUsage;

                    return modelResult;
                }


            }
            if (!searchDto.ToggleDB)
            {

                List<DateOnly> dateList = new List<DateOnly>();

                for (DateOnly date = searchDto.FromDate; date <= searchDto.ToDate; date = date.AddDays(1))
                {
                    dateList.Add(date);
                }

                List<WeatherDataDto> weatherDataDtoList = new List<WeatherDataDto>();


                // start measuring CPU usage and Memory before executing the code
                (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPU();
                long currentBytes = _utilityManager.BeginMeasureMemory();
                List<BinaryDataFromFileDto> listOfBinaryDataFromFileDto = await _locationRepository.FetchAddressByCoordinates(searchDto);
                List<BinarySearchInFilesDto> binarySearchInFilesDtos = new List<BinarySearchInFilesDto>();

                try
                {
                    for (int i = 0; i < dateList.Count; i++)
                    {

                        double yearDate = _utilityManager.ConvertDateTimeToFloatInternal(dateList[i].ToString());
                        var fullDate = _utilityManager.MixedYearDateTimeSplitter(yearDate)[0].ToString(); //contains the date format YYYYMMDD
                        var year = fullDate.Substring(0, 4);
                        var monthDay = fullDate.Substring(4, 4);
                        var directory = Path.Combine(_baseDirectory, year);
                        var fileName = Path.Combine(directory, $"{monthDay}.bin");
                        BinarySearchInFilesDto binaryDataFromFileDto = new BinarySearchInFilesDto();

                        try
                        {
                            for (int j = 0; j < listOfBinaryDataFromFileDto.Count; j++)
                            {

                                binaryDataFromFileDto.FilePath = fileName;
                                binaryDataFromFileDto.FromByte = (listOfBinaryDataFromFileDto[j].LocationId - 1) * 960;
                                binaryDataFromFileDto.ToByte = listOfBinaryDataFromFileDto[j].LocationId * 960 - 1;

                                binarySearchInFilesDtos.Add(binaryDataFromFileDto);

                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"inner loop: {e}");

                        }

                    }
                }
                catch (Exception e)
                {

                    Debug.WriteLine($"outer loop: {e}");
                }



                var result = await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDtos);


                //  listOfBinaryDataFromFileDto = await _crudFileRepository.FetchWeatherDataAsync(_baseDirectory, searchDto);


                // return recorded CPU usage and memory usage
                var cpuResult = _utilityManager.StopMeasureCPU(cpuTimeBefore, stopwatch);
                var byteMemory = _utilityManager.StopMeasureMemory(currentBytes);
                string measuredRamUsage = _utilityManager.ConvertBytesToFormat(byteMemory);

                for (int i = 0; i < listOfBinaryDataFromFileDto.Count; i++)
                {
                    string[] coordinateParts = listOfBinaryDataFromFileDto[i].Coordinates.Split('-');


                    WeatherDataFileDto weatherDataFileDto = _binaryConversionManager.ConvertBinaryDataToWeatherDataFileDto(listOfBinaryDataFromFileDto[i].BinaryWeatherData);

                    for (int j = 0; j < weatherDataFileDto.Time.Length; j++)
                    {
                        WeatherDataDto weatherDataDto = new WeatherDataDto();
                        weatherDataDto.Address = listOfBinaryDataFromFileDto[i].Address;
                        weatherDataDto.Latitude = coordinateParts[0];
                        weatherDataDto.Longitude = coordinateParts[1];
                        weatherDataDto.TemperatureC = weatherDataFileDto.Temperature_2m[j];
                        weatherDataDto.RelativeHumidity = weatherDataFileDto.Relative_Humidity_2m[j];
                        weatherDataDto.Rain = weatherDataFileDto.Rain[j];
                        weatherDataDto.WindSpeed = weatherDataFileDto.Wind_Speed_10m[j];
                        weatherDataDto.WindDirection = weatherDataFileDto.Wind_Direction_10m[j];
                        weatherDataDto.WindGust = weatherDataFileDto.Wind_Gusts_10m[j];
                        weatherDataDto.GlobalTiltedIrRadiance = weatherDataFileDto.Global_Tilted_Irradiance_Instant[j];
                        weatherDataDto.DateAndTime = DateTime.ParseExact(string.Concat(listOfBinaryDataFromFileDto[i].YearDate, weatherDataFileDto.Time[j]), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
                        weatherDataDtoList.Add(weatherDataDto);
                    }
                }

                for (int i = 0; i < weatherDataDtoList.Count; i++)
                {
                    weatherDataDtoList[i] = _solarPositionManager.CalculateSunAngles(weatherDataDtoList[i]);
                }
                metaDataDto.WeatherData = weatherDataDtoList;

                //calculate amount of data
                int weatherDataInBytes = _utilityManager.GetModelSize(metaDataDto.WeatherData);
                int metaDataModelInBytes = _utilityManager.GetModelSize(metaDataDto);
                int totalBytes = metaDataModelInBytes + weatherDataInBytes;
                string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);

                //map measurements to model
                metaDataDto.DataLoadedMB = dataCollectedInMB;
                metaDataDto.FetchDataTimer = cpuResult.ElapsedTimeMs;
                metaDataDto.CpuUsage = cpuResult.CpuUsage;
                metaDataDto.RamUsage = measuredRamUsage;

                return metaDataDto;
            }

            return null;
        }
    }
}
