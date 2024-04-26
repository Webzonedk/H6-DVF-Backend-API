﻿using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;

namespace DVF_API.Services.ServiceImplementation
{
    public class DataService_old : IDataService
    {

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";


        private readonly ICrudDatabaseRepository _crudDatabaseRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICrudFileRepository _crudFileRepository;
        private readonly IBinaryConversionManager _binaryConversionManager;
        private readonly ISolarPositionManager _solarPositionManager;

        public DataService_old(
            ICrudDatabaseRepository crudDatabaseRepository, ILocationRepository locationRepository,
            ICrudFileRepository crudFileRepository, IBinaryConversionManager binaryConversionManager,
            ISolarPositionManager solarPositionManager)
        {
            _crudDatabaseRepository = crudDatabaseRepository;
            _locationRepository = locationRepository;
            _crudFileRepository = crudFileRepository;
            _binaryConversionManager = binaryConversionManager;
            _solarPositionManager = solarPositionManager;
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
                // start måling
                MetaDataDto modelResult = await _crudDatabaseRepository.FetchWeatherDataAsync(searchDto);
                return null;
                // slut måling
                //calculate sun og tilføj målingsresultater
            }
            if (!searchDto.ToggleDB)
            {
                List<WeatherDataDto> weatherDataDtoList = new List<WeatherDataDto>();

                List<BinaryDataFromFileDto> listOfBinaryDataFromFileDto = await _crudFileRepository.FetchWeatherDataAsync(_baseDirectory, searchDto);
                listOfBinaryDataFromFileDto = await _locationRepository.FetchAddressByCoordinates(listOfBinaryDataFromFileDto);
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
            }
            return new MetaDataDto();
        }
    }
}