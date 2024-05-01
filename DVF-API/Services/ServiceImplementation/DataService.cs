using DVF_API.Data.Interfaces;
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
        private readonly ISolarPositionManager _solarPositionManager;
        private readonly IUtilityManager _utilityManager;

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
                long byteMemory = _utilityManager.StopMeasureMemory(currentBytes);
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
                            var result = _solarPositionManager.CalculateSunAngles(modelResult.WeatherData[index].DateAndTime, double.Parse( modelResult.WeatherData[index].Latitude), double.Parse( modelResult.WeatherData[index].Longitude));
                            modelResult.WeatherData[index].SunAzimuthAngle = (float)result.SunAzimuth;
                            modelResult.WeatherData[index].SunElevationAngle = (float)result.SunAltitude;
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

                            BinaryWeatherStructDto[] result = await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDto);


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
                                await Console.Out.WriteLineAsync($"exception in locations dictioanry: {e}");

                            }




                            long Id = 0;
                            float time = 0;
                            string _date = "";
                            string? _year = "";
                            
                            foreach (var weatherDataBlock in result)
                            {
                                WeatherDataDto historicWeatherDataToFileDto = new WeatherDataDto();

                                unsafe
                                {
                                    BinaryWeatherStructDto datablock = weatherDataBlock;
                                    Id = datablock.LocationId;
                                    time = datablock.WeatherData[0];
                                    _date = Path.GetFileNameWithoutExtension(fileName);
                                    _year = Path.GetFileName(Path.GetDirectoryName(fileName));

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
                                    var sunResult = _solarPositionManager.CalculateSunAngles(historicWeatherDataToFileDto.DateAndTime, double.Parse( historicWeatherDataToFileDto.Latitude.Replace(".",",")),double.Parse( historicWeatherDataToFileDto.Longitude.Replace(".", ",")));
                                    historicWeatherDataToFileDto.SunAzimuthAngle = (float)sunResult.SunAzimuth;
                                    historicWeatherDataToFileDto.SunElevationAngle = (float)sunResult.SunAltitude;
                                }

                               

                                weatherDataDtoList.Add(historicWeatherDataToFileDto);
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



                //var result = await _crudFileRepository.FetchWeatherDataAsync(binarySearchInFilesDtoDictionary);



                // return recorded CPU usage and memory usage
                var cpuResult = _utilityManager.StopMeasureCPU(cpuTimeBefore, stopwatch);
                var byteMemory = _utilityManager.StopMeasureMemory(currentBytes);
                string measuredRamUsage = _utilityManager.ConvertBytesToFormat(byteMemory);

                //Dictionary<long, LocationDto> locations = new Dictionary<long, LocationDto>();

                (TimeSpan convertionCpuTimeBefore, Stopwatch convertionStopwatch) = _utilityManager.BeginMeasureCPU();
                long convertionCurrentBytes = _utilityManager.BeginMeasureMemory();

                ////get all cooordinates
                //try
                //{
                //    if (totalCoordinates != 1)
                //    {
                //        locations = await _locationRepository.GetAllLocationCoordinates();
                //    }
                //    else
                //    {
                //        var location = await _locationRepository.FetchAddressByCoordinates(searchDto);
                //        int locationId = location[0].LocationId;
                //        var coordinateDictionary = await _locationRepository.FetchLocationCoordinates(locationId, locationId);
                //        var address = location[0].Address.Split(' ');
                //        var coordinates = coordinateDictionary[locationId].Split("-");
                //        LocationDto locationDto = new LocationDto()
                //        {
                //            Latitude = coordinates[0],
                //            Longitude = coordinates[1],
                //            StreetName = address[0],
                //            StreetNumber = address[1],
                //            PostalCode = address[2],
                //            CityName = address[3]
                //        };
                //        locations.Add(locationId, locationDto);
                //    }
                //}
                //catch (Exception e)
                //{
                //    await Console.Out.WriteLineAsync($"exception in locations dictioanry: {e}");

                //}


                //List<Task> tasks = new List<Task>();
                //long Id = 0;
                //float time = 0;
                //string _date = "";
                //string? _year = "";
                //foreach (var weatherDataBlock in result)
                //{
                //    WeatherDataDto historicWeatherDataToFileDto = new WeatherDataDto();

                //    unsafe
                //    {
                //        BinaryWeatherStructDto datablock = weatherDataBlock.Value;
                //        Id = datablock.LocationId;
                //        time = datablock.WeatherData[0];
                //        _date = Path.GetFileNameWithoutExtension(weatherDataBlock.Key);
                //        _year = Path.GetFileName(Path.GetDirectoryName(weatherDataBlock.Key));


                //        historicWeatherDataToFileDto.DateAndTime = DateTime.ParseExact(string.Concat(_year, _date, time.ToString("0000")), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
                //        historicWeatherDataToFileDto.Address = $"{locations[Id].StreetName} {locations[Id].StreetNumber}, {locations[Id].PostalCode} {locations[Id].CityName}";
                //        historicWeatherDataToFileDto.Latitude = locations[Id].Latitude;
                //        historicWeatherDataToFileDto.Longitude = locations[Id].Longitude;
                //        historicWeatherDataToFileDto.TemperatureC = datablock.WeatherData[0];
                //        historicWeatherDataToFileDto.RelativeHumidity = datablock.WeatherData[1];
                //        historicWeatherDataToFileDto.Rain = datablock.WeatherData[2];
                //        historicWeatherDataToFileDto.WindSpeed = datablock.WeatherData[3];
                //        historicWeatherDataToFileDto.WindDirection = datablock.WeatherData[4];
                //        historicWeatherDataToFileDto.WindGust = datablock.WeatherData[5];
                //        historicWeatherDataToFileDto.GlobalTiltedIrRadiance = datablock.WeatherData[6];

                //    }

                //    WeatherDataDto weatherDataDtoCopy = historicWeatherDataToFileDto;
                //    tasks.Add(Task.Run(() =>
                //    {
                //        _solarPositionManager.CalculateSunAngles(weatherDataDtoCopy);
                //    }));

                //    weatherDataDtoList.Add(historicWeatherDataToFileDto);


                //}
                //await Task.WhenAll(tasks);

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
                metaDataDto.CpuUsage = cpuResult.CpuUsagePercentage;
                metaDataDto.RamUsage = measuredRamUsage;
                metaDataDto.ConvertionTimer = _utilityManager.ConvertTimeMeasurementToFormat(convertionCpuResult.ElapsedTimeMs);
                metaDataDto.ConvertionCpuUsage = convertionCpuResult.CpuUsagePercentage;
                metaDataDto.ConvertionRamUsage = convertionMeasuredRamUsage;

                return metaDataDto;
            }

            return null;
        }
    }
}
