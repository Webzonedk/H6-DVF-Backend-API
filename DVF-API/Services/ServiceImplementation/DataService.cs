﻿using DVF_API.Data.Interfaces;
using DVF_API.Domain.BusinessLogic;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
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

                    (TimeSpan convertionCpuTimeBefore, Stopwatch convertionStopwatch) = _utilityManager.BeginMeasureCPU();
                    long convertionCurrentBytes = _utilityManager.BeginMeasureMemory();


                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < modelResult.WeatherData.Count; i++)
                    {
                        int index = i;
                        tasks.Add(Task.Run(() =>
                        {
                            var result = _solarPositionManager.CalculateSunAngles(modelResult.WeatherData[index]);
                            modelResult.WeatherData[index] = result;
                        }));
                    }

                    await Task.WhenAll(tasks);


                    var ConvertioncpuResult = _utilityManager.StopMeasureCPU(convertionCpuTimeBefore, convertionStopwatch);
                    var ConvertionbyteMemory = _utilityManager.StopMeasureMemory(convertionCurrentBytes);
                    string ConvertionmeasuredRamUsage = _utilityManager.ConvertBytesToFormat(ConvertionbyteMemory);


                    //calculate amount of data
                    int metaDataModelInBytes = _utilityManager.GetModelSize(modelResult);
                    int totalBytes = metaDataModelInBytes;
                    string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);

                    //map measurements to model
                    modelResult.DataLoadedMB = dataCollectedInMB;
                    modelResult.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(cpuResult.ElapsedTimeMs);
                    modelResult.CpuUsage = cpuResult.CpuUsage;
                    modelResult.RamUsage = measuredRamUsage;
                    modelResult.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(ConvertioncpuResult.ElapsedTimeMs);
                    modelResult.ConvertionCpuUsage = ConvertioncpuResult.CpuUsage;
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
                                BinarySearchInFilesDto binarySearchInFilesDto = new BinarySearchInFilesDto();
                                binarySearchInFilesDto.FromByte = (listOfBinaryDataFromFileDto[j].LocationId - 1) * 960;
                                binarySearchInFilesDto.ToByte = listOfBinaryDataFromFileDto[j].LocationId * 960 - 1;

                                if (binarySearchInFilesDtoDictionary.ContainsKey(fileName))
                                {
                                    binarySearchInFilesDtoDictionary[fileName].Add(binarySearchInFilesDto);
                                }
                                else
                                {
                                    binarySearchInFilesDtoDictionary.Add(fileName, new List<BinarySearchInFilesDto>());
                                    binarySearchInFilesDtoDictionary[fileName].Add(binarySearchInFilesDto);
                                }

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

                // start measuring CPU usage and Memory before executing the code
                (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPU();
                long currentBytes = _utilityManager.BeginMeasureMemory();

                var result = await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDtoDictionary);


                // return recorded CPU usage and memory usage
                var cpuResult = _utilityManager.StopMeasureCPU(cpuTimeBefore, stopwatch);
                var byteMemory = _utilityManager.StopMeasureMemory(currentBytes);
                string measuredRamUsage = _utilityManager.ConvertBytesToFormat(byteMemory);

                Dictionary<long, LocationDto> locations = new Dictionary<long, LocationDto>();

                (TimeSpan convertionCpuTimeBefore, Stopwatch convertionStopwatch) = _utilityManager.BeginMeasureCPU();
                long convertionCurrentBytes = _utilityManager.BeginMeasureMemory();

                //get all cooordinates
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

                List<Task> tasks = new List<Task>();
                long Id = 0;
                float time = 0;
                string _date = "";
                string? _year = "";
                unsafe
                {
                    foreach (var weatherDataBlock in result)
                    {
                        BinaryWeatherStructDto datablock = weatherDataBlock.Value;
                        Id = datablock.LocationId;
                        time = datablock.WeatherData[0];
                        _date = Path.GetFileNameWithoutExtension(weatherDataBlock.Key);
                        _year = Path.GetFileName(Path.GetDirectoryName(weatherDataBlock.Key));

                        WeatherDataDto historicWeatherDataToFileDto = new WeatherDataDto();

                        historicWeatherDataToFileDto.DateAndTime = DateTime.ParseExact(string.Concat(_year, _date, time.ToString("0000")), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
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


                        WeatherDataDto weatherDataDtoCopy = historicWeatherDataToFileDto;
                        tasks.Add(Task.Run(() =>
                        {
                            _solarPositionManager.CalculateSunAngles(weatherDataDtoCopy);
                        }));

                        weatherDataDtoList.Add(historicWeatherDataToFileDto);


                    }
                }
                await Task.WhenAll(tasks);

                metaDataDto.WeatherData = weatherDataDtoList;

                // return recorded CPU usage and memory usage
                var convertionCpuResult = _utilityManager.StopMeasureCPU(convertionCpuTimeBefore, convertionStopwatch);
                var convertionByteMemory = _utilityManager.StopMeasureMemory(convertionCurrentBytes);
                string convertionMeasuredRamUsage = _utilityManager.ConvertBytesToFormat(convertionByteMemory);


                //calculate amount of data
                int metaDataModelInBytes = _utilityManager.GetModelSize(metaDataDto);
                int totalBytes = metaDataModelInBytes;
                string dataCollectedInMB = _utilityManager.ConvertBytesToFormat(totalBytes);

                //map measurements to model
                metaDataDto.DataLoadedMB = dataCollectedInMB;
                metaDataDto.FetchDataTimer = _utilityManager.ConvertTimeMeasurementToFormat(cpuResult.ElapsedTimeMs);
                metaDataDto.CpuUsage = cpuResult.CpuUsage;
                metaDataDto.RamUsage = measuredRamUsage;
                metaDataDto.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(convertionCpuResult.ElapsedTimeMs);
                metaDataDto.ConvertionCpuUsage = convertionCpuResult.CpuUsage;
                metaDataDto.ConvertionRamUsage = convertionMeasuredRamUsage;

                return metaDataDto;
            }

            return null;
        }
    }
}
