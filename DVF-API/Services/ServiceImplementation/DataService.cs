using DVF_API.Data.Interfaces;
using DVF_API.Domain.BusinessLogic;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;



namespace DVF_API.Services.ServiceImplementation
{
    public class DataService : IDataService
    {

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";


        private readonly ICrudDatabaseRepository _crudDatabaseRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICrudFileRepository _crudFileRepository;
        private readonly ISolarPositionManager _solarPositionManager;
        private readonly IUtilityManager _utilityManager;


        private (float CpuUsagePercentage, float ElapsedTimeMs) cpuResult;
        private long _byteMemory;
        private TimeSpan _convertionCpuTimeBefore;
        private Stopwatch _convertionStopwatch = new Stopwatch();
        private TimeSpan _cpuTimeBefore;
        private Stopwatch _stopwatch = new Stopwatch();
        private long _convertionCurrentBytes;
        private (float CpuUsagePercentage, float ElapsedTimeMs) _convertionCpuResult;
        private long _convertionByteMemory;
        private long _currentBytes;


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
                // start measuring CPU usage and Memory before executing the code
                (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPUTime();
                long currentBytes = _utilityManager.BeginMeasureMemory();



                //get data
                MetaDataDto modelResult = await _crudDatabaseRepository.FetchWeatherDataAsync(searchDto);


                // return recorded CPU usage and memory usage
                cpuResult = _utilityManager.StopMeasureCPUTime(cpuTimeBefore, stopwatch);
                _byteMemory = _utilityManager.StopMeasureMemory(currentBytes);



                //map data to model if model is retrieved successfully
                if (modelResult != null)
                {

                    (TimeSpan convertionCpuTimeBefore, Stopwatch convertionStopwatch) = _utilityManager.BeginMeasureCPUTime();
                    long convertionCurrentBytes = _utilityManager.BeginMeasureMemory();


                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < modelResult.WeatherData.Count; i++)
                    {
                        int index = i;
                        tasks.Add(Task.Run(() =>
                        {
                            var result = _solarPositionManager.CalculateSunAngles(modelResult.WeatherData[index].DateAndTime, double.Parse(modelResult.WeatherData[index].Latitude), double.Parse(modelResult.WeatherData[index].Longitude));
                            modelResult.WeatherData[index].SunAzimuthAngle = (float)result.SunAzimuth;
                            modelResult.WeatherData[index].SunElevationAngle = (float)result.SunAltitude;
                        }));
                    }

                    await Task.WhenAll(tasks);


                    var ConvertioncpuResult = _utilityManager.StopMeasureCPUTime(convertionCpuTimeBefore, convertionStopwatch);
                    var ConvertionbyteMemory = _utilityManager.StopMeasureMemory(convertionCurrentBytes);
                    string ConvertionmeasuredRamUsage = _utilityManager.ConvertBytesToFormat(ConvertionbyteMemory);

                    //calculate amount of data
                    int metaDataModelInBytes = _utilityManager.GetModelSize(modelResult);
                    int totalBytes = metaDataModelInBytes;
                    string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);
                    string measuredRamUsage = _utilityManager.ConvertBytesToFormat(_byteMemory);

                    //map measurements to model
                    modelResult.DataLoadedMB = dataCollectedInMB;
                    modelResult.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(cpuResult.ElapsedTimeMs);
                    modelResult.CpuUsage = cpuResult.CpuUsagePercentage;
                    modelResult.RamUsage = measuredRamUsage;
                    modelResult.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(ConvertioncpuResult.ElapsedTimeMs);
                    modelResult.ConvertionCpuUsage = ConvertioncpuResult.CpuUsagePercentage;
                    modelResult.ConvertionRamUsage = ConvertionmeasuredRamUsage;

                    return modelResult;
                }

            }
            if (!searchDto.ToggleDB)
            {

                List<DateOnly> dateList = new List<DateOnly>();
                int totalCoordinates = searchDto.Coordinates.Count;

                for (DateOnly date = searchDto.FromDate; date <= searchDto.ToDate; date = date.AddDays(1))
                {
                    dateList.Add(date);
                }





                List<WeatherDataDto> weatherDataDtoList = new List<WeatherDataDto>();
                //get locations before getting weatherdata
                List<BinaryDataFromFileDto> listOfBinaryDataFromFileDto = await _locationRepository.FetchAddressByCoordinates(searchDto);
                Dictionary<string, List<BinarySearchInFilesDto>> binarySearchInFilesDtoDictionary = new Dictionary<string, List<BinarySearchInFilesDto>>();


                try
                {
                   

                    for (int i = 0; i < dateList.Count; i++)
                    {
                        // start measuring CPU usage and Memory before executing the code
                         (_cpuTimeBefore, _stopwatch) = _utilityManager.BeginMeasureCPUTime();
                      //  _utilityManager.BeginMeasureCPU(_stopwatch);
                        _currentBytes = _utilityManager.BeginMeasureMemory();

                        double yearDate = _utilityManager.ConvertDateTimeToDouble(dateList[i].ToString());
                        var fullDate = _utilityManager.MixedYearDateTimeSplitter(yearDate)[0].ToString(); //contains the date format YYYYMMDD
                        var year = fullDate.Substring(0, 4);
                        var monthDay = fullDate.Substring(4, 4);
                        var directory = Path.Combine(_baseDirectory, year);
                        var fileName = Path.Combine(directory, $"{monthDay}.bin");
                        BinarySearchInFilesDto binarySearchInFilesDto = new BinarySearchInFilesDto();
                        try
                        {
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

                            BinaryWeatherStructDto[] returnedStructArrayWithWeatherData = await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDto);


                            // return recorded CPU usage and memory usage
                            var tempCpuResult = _utilityManager.StopMeasureCPUTime(_cpuTimeBefore, _stopwatch);
                         //   var tempCpuResult = _utilityManager.StopMeasureCPU(_stopwatch);
                            cpuResult.ElapsedTimeMs += tempCpuResult.ElapsedTimeMs;
                            cpuResult.CpuUsagePercentage += tempCpuResult.CpuUsagePercentage;
                            _byteMemory += _utilityManager.StopMeasureMemory(_currentBytes);
                            _stopwatch.Restart();
                         

                            Dictionary<long, LocationDto> locations = new Dictionary<long, LocationDto>();
                            //get all cooordinates
                            try
                            {
                                if (totalCoordinates != 1)
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



                            long Id = 0;
                            float time = 0;
                            string tempDate = "";
                            string? tempYear = "";


                            (_convertionCpuTimeBefore, _convertionStopwatch) = _utilityManager.BeginMeasureCPUTime();
                            _convertionCurrentBytes = _utilityManager.BeginMeasureMemory();

                            foreach (var weatherStruct in returnedStructArrayWithWeatherData)
                            {
                                WeatherDataDto historicWeatherDataToFileDto = new WeatherDataDto();

                                unsafe
                                {
                                    BinaryWeatherStructDto datablock = weatherStruct;
                                    Id = datablock.LocationId;
                                    time = datablock.WeatherData[0];
                                    tempDate = Path.GetFileNameWithoutExtension(fileName);
                                    tempYear = Path.GetFileName(Path.GetDirectoryName(fileName));

                                    historicWeatherDataToFileDto.DateAndTime = DateTime.ParseExact(string.Concat(tempYear, tempDate, time.ToString("0000")), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
                                    historicWeatherDataToFileDto.Address = $"{locations[Id].StreetName} {locations[Id].StreetNumber}, {locations[Id].PostalCode} {locations[Id].CityName}";
                                    historicWeatherDataToFileDto.Latitude = locations[Id].Latitude;
                                    historicWeatherDataToFileDto.Longitude = locations[Id].Longitude;
                                    historicWeatherDataToFileDto.TemperatureC = datablock.WeatherData[0];
                                    historicWeatherDataToFileDto.RelativeHumidity = datablock.WeatherData[1];
                                    historicWeatherDataToFileDto.Rain = datablock.WeatherData[2];
                                    historicWeatherDataToFileDto.WindSpeed = datablock.WeatherData[3];
                                    historicWeatherDataToFileDto.WindDirection = datablock.WeatherData[4];
                                    historicWeatherDataToFileDto.WindGust = datablock.WeatherData[5];
                                    historicWeatherDataToFileDto.GlobalTiltedIrRadiance = datablock.WeatherData[6];
                                    var sunResult = _solarPositionManager.CalculateSunAngles(historicWeatherDataToFileDto.DateAndTime, double.Parse(historicWeatherDataToFileDto.Latitude, CultureInfo.InvariantCulture), double.Parse(historicWeatherDataToFileDto.Longitude, CultureInfo.InvariantCulture));
                                    historicWeatherDataToFileDto.SunAzimuthAngle = (float)sunResult.SunAzimuth;
                                    historicWeatherDataToFileDto.SunElevationAngle = (float)sunResult.SunAltitude;
                                }



                                weatherDataDtoList.Add(historicWeatherDataToFileDto);
                            }

                            // return recorded CPU usage and memory usage
                            _convertionByteMemory += _utilityManager.StopMeasureMemory(_convertionCurrentBytes);
                            var tempVonvertionCpuResult = _utilityManager.StopMeasureCPUTime(_convertionCpuTimeBefore, _convertionStopwatch);
                          //  var tempVonvertionCpuResult = _utilityManager.StopMeasureCPU(_convertionStopwatch);
                            _convertionCpuResult.ElapsedTimeMs += tempVonvertionCpuResult.ElapsedTimeMs;
                            _convertionCpuResult.CpuUsagePercentage += tempVonvertionCpuResult.CpuUsagePercentage;

                        }
                        catch (Exception ex)
                        {
                            metaDataDto.ResponseMessage = $"En fejl opstod under datahentning i inner loopet: {ex.Message}";
                        }
                    }


                   


                }
                catch (Exception ex)
                {
                    metaDataDto.ResponseMessage = $"En fejl opstod under datahentning: {ex.Message}";
                }




                metaDataDto.WeatherData = new List<WeatherDataDto>();
                metaDataDto.WeatherData.AddRange(weatherDataDtoList);
                weatherDataDtoList.Clear();

                //calculate amount of data retrieved
                int metaDataModelInBytes = _utilityManager.GetModelSize(metaDataDto);
                int totalBytes = metaDataModelInBytes;
                string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);
              
                //measured Ram usage
                string measuredCollectedDataRamUsage = _utilityManager.ConvertBytesToFormat(_byteMemory);
                string convertionMeasuredRamUsage = _utilityManager.ConvertBytesToFormat(_convertionByteMemory);

                //measured time usage
                float dataCollectedTimer = cpuResult.ElapsedTimeMs;
                float convertedDataTimer = _convertionCpuResult.ElapsedTimeMs;

                //measured Cpu usage
                float collectedDataCpuUsage = cpuResult.CpuUsagePercentage;
                float convertedDataCpuUsage = _convertionCpuResult.CpuUsagePercentage;

                //map measurements to model
                metaDataDto.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(dataCollectedTimer);
                metaDataDto.DataLoadedMB = dataCollectedInMB;
                metaDataDto.RamUsage = measuredCollectedDataRamUsage;
                metaDataDto.CpuUsage = collectedDataCpuUsage;
                metaDataDto.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(convertedDataTimer);
                metaDataDto.ConvertionRamUsage = convertionMeasuredRamUsage;
                metaDataDto.ConvertionCpuUsage = convertedDataCpuUsage;

                return metaDataDto;
            }

            return null;
        }
    }
}
