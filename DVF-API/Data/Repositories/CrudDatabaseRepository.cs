using DVF_API.Data.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;
using System;
using DVF_API.Data.Models;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using DVF_API.Domain.Interfaces;
using DVF_API.Domain.BusinessLogic;


namespace DVF_API.Data.Repositories
{
    public class CrudDatabaseRepository : ICrudDatabaseRepository, ILocationRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IUtilityManager _utilityManager;
        private readonly ISolarPositionManager _solarPositionManager;

        public CrudDatabaseRepository(IConfiguration configuration, IUtilityManager utilityManager, ISolarPositionManager solarPositionManager)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb");
            _utilityManager = utilityManager;
            _solarPositionManager = solarPositionManager;
        }
        //    private readonly IDatabaseRepository _databaseRepository;
        //  private readonly ILocationRepository _locationRepository;
        //  private readonly IConfiguration _configuration;
        //  private readonly string _connectionString;

        //public CrudDatabaseRepository(IDatabaseRepository databaseRepository, IConfiguration configuration, ILocationRepository locationRepository)
        //  {
        //      _locationRepository = locationRepository;
        //      _databaseRepository = databaseRepository;
        //      _configuration = configuration;
        //      _connectionString = _configuration.GetConnectionString("WeatherDataDb");
        //  }


        public async Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT WD.*,C.CityName,c.PostalCode,L.StreetName,L.StreetNumber,L.Latitude,L.Longitude" +
                " FROM WeatherDatas WD" +
                " JOIN Locations L ON WD.LocationId = L.LocationId" +
                " JOIN Cities C ON L.CityId = C.CityId" +
                " WHERE WD.DateAndTime >= @FromDate" +
                " AND WD.DateAndTime <= @ToDate" +
                " AND L.Latitude = @Latitude" +
                " AND L.Longitude = @Longitude" +
                " AND WD.IsDeleted = 0";

            await using SqlCommand command = new SqlCommand(query, connection);

            CultureInfo culture = new CultureInfo("en-US");
            string formattedToDate = searchDto.ToDate.ToString("yyyy-MM-dd HH:mm:ss", culture);
            string formattedFromDate = searchDto.FromDate.ToString("yyyy-MM-dd HH:mm:ss", culture);

            command.Parameters.AddWithValue("@FromDate ", formattedFromDate);
            command.Parameters.AddWithValue("@ToDate", formattedToDate);

            command.Parameters.Add("@Latitude", SqlDbType.VarChar, 255);
            command.Parameters.Add("@Longitude", SqlDbType.VarChar, 255);
            //command.Parameters["@FromDate"].Value = searchDto.FromDate;
            //command.Parameters["@ToDate"].Value = searchDto.ToDate;

            // Get CPU usage before executing the code
            (TimeSpan cpuTimeBefore, Stopwatch stopwatch) = _utilityManager.BeginMeasureCPU();

            //measure Memory
            (Process currentProcess, long currentBytes) = _utilityManager.BeginMeasureMemory();

            List<WeatherDataDto> weatherData = new List<WeatherDataDto>();
            foreach (string coordinates in searchDto.Coordinates)
            {
                string latitude = coordinates.Split('-')[0];
                string longitude = coordinates.Split("-")[1];

                command.Parameters["@Latitude"].Value = latitude;
                command.Parameters["@Longitude"].Value = longitude;

                try
                {
                    var result = await command.ExecuteReaderAsync();

                    while (await result.ReadAsync())
                    {
                        WeatherDataDto data = new WeatherDataDto()
                        {
                            Address = $"{result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}",
                            Latitude = result["Latitude"].ToString(),
                            Longitude = result["Longitude"].ToString(),
                            TemperatureC = Convert.ToSingle(result["TemperatureC"]),
                            WindSpeed = Convert.ToSingle(result["WindSpeed"]),
                            WindDirection = Convert.ToSingle(result["WindDirection"]),
                            WindGust = Convert.ToSingle(result["WindGust"]),
                            RelativeHumidity = Convert.ToSingle(result["RelativeHumidity"]),
                            Rain = Convert.ToSingle(result["Rain"]),
                            GlobalTiltedIrRadiance = Convert.ToSingle(result["GlobalTiltedIrRadiance"]),
                            DateAndTime = Convert.ToDateTime(result["DateAndTime"]),
                        };
                        data = _solarPositionManager.CalculateSunAngles(data);
                        weatherData.Add(data);
                    }
                    result.Close();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred: {ex.Message}");
                    stopwatch.Stop();
                    // Ready for logging
                }
            }




            // return recorded CPU usage
            var cpuResult = _utilityManager.StopMeasureCPU(cpuTimeBefore, stopwatch);

            //return recorded Memory usage
            var memory = _utilityManager.StopMeasureMemory(currentBytes, currentProcess);


            MetaDataDto metaDatamodel = new MetaDataDto()
            {
                FetchDataTimer = cpuResult.ElapsedTimeMs,
                RamUsage = memory,
                CpuUsage = cpuResult.CpuUsage,
                WeatherData = weatherData
            };

            //calculate amount of data
            int weatherDataInBytes = _utilityManager.GetModelSize(weatherData);
            int metaDataModelInBytes = _utilityManager.GetModelSize(metaDatamodel);
            int totalBytes = metaDataModelInBytes + weatherDataInBytes;
            float dataCollectedInMB = _utilityManager.ConvertBytesToMegabytes(totalBytes);

            metaDatamodel.DataLoadedMB = dataCollectedInMB;
            return metaDatamodel;

        }

        public async Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE WD SET WD.IsDeleted = 1" +
                " FROM WeatherDatas WD" +
                " JOIN Locations L ON WD.LocationId = L.LocationId" +
                " WHERE WD.DateAndTime < @beforeDate";

            await using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@beforeDate", deleteWeatherDataBeforeThisDate);

            try
            {
                var result = await command.ExecuteNonQueryAsync();
                if (result > 0)
                {
                    Debug.WriteLine("data successfully deleted");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
            }

        }

        public async Task RestoreAllData()
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE WeatherDatas SET WeatherDatas.IsDeleted = 0 FROM WeatherDatas WHERE WeatherDatas.IsDeleted = 1";
            await using SqlCommand command = new SqlCommand(query, connection);

            try
            {
                var result = await command.ExecuteNonQueryAsync();
                if (result > 0)
                {
                    Debug.WriteLine("data successfully restored");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
            }
        }

        public async Task<List<string>> FetchLocationCoordinates(int fromIndex, int toIndex)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM Locations WHERE LocationId BETWEEN @fromIndex AND @toIndex";
            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@fromIndex", fromIndex);
            command.Parameters.AddWithValue("@toIndex", toIndex);

            try
            {
                var result = await command.ExecuteReaderAsync();
                List<string> coordinates = new List<string>();

                while (await result.ReadAsync())
                {
                    string latitude = result["Latitude"].ToString();
                    string longitude = result["Longitude"].ToString();

                    string coordinate = $"{latitude}-{longitude}";
                    coordinates.Add(coordinate);
                }
                return coordinates;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
            }
            return null;
        }

        public async Task<int> FetchLocationCount()
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Locations";
            await using SqlCommand command = new SqlCommand(query, connection);

            try
            {
                var result = await command.ExecuteReaderAsync();
                if (await result.ReadAsync())
                {
                    return result.GetInt32(0);

                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
                return 0;
            }

        }

        public async Task<List<string>> FetchMatchingAddresses(string partialAddress)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT Locations.StreetName, Locations.StreetNumber, Cities.PostalCode, Cities.CityName FROM Locations" +
               " JOIN Cities ON Locations.CityId = Cities.CityId" +
               " WHERE (Locations.StreetName + ' ' + Locations.StreetNumber LIKE @searchCriteria" +
               " OR Cities.PostalCode LIKE @searchCriteria OR Cities.CityName LIKE @searchCriteria)";


            await using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@searchCriteria", partialAddress);

            try
            {
                List<string> addresses = new List<string>();

                var result = await command.ExecuteReaderAsync();
                while (await result.ReadAsync())
                {
                    // Combine columns into a single string with the specified format for each row
                    string combinedAddress = $"{result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}";

                    // Add the combined string to the list
                    addresses.Add(combinedAddress);
                }

                return addresses;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
                return null; // Or any other default value in case of an error
            }
        }

        public async Task InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "DECLARE @LocationId INT;" +
                "SELECT @LocationId = LocationId FROM Locations WHERE Latitude = @Latitude AND Longitude = @Longitude; " +
                "INSERT INTO WeatherDatas(TemperatureC, WindSpeed, WindDirection, WindGust, RelativeHumidity, Rain, GlobalTiltedIrRadiance, DateAndTime, LocationId)" +
                "VALUES(@Temperature, @WindSpeed, @WindDirection, @WindGust, @RelativeHumidity, @Rain, @GlobalTiltedIrRadiance, @DateAndTime, @LocationId)";

            await using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Latitude", weatherDataFromIOT.Latitude);
            command.Parameters.AddWithValue("@Longitude", weatherDataFromIOT.Longitude);
            command.Parameters.AddWithValue("@Temperature", weatherDataFromIOT.Temperature);
            command.Parameters.AddWithValue("@WindSpeed", weatherDataFromIOT.WindSpeed);
            command.Parameters.AddWithValue("@WindDirection", weatherDataFromIOT.WindDirection);
            command.Parameters.AddWithValue("@WindGust", weatherDataFromIOT.WindGust);
            command.Parameters.AddWithValue("@RelativeHumidity", weatherDataFromIOT.RelativeHumidity);
            command.Parameters.AddWithValue("@Rain", weatherDataFromIOT.Rain);
            command.Parameters.AddWithValue("@GlobalTiltedIrRadiance", weatherDataFromIOT.GlobalTiltedIrRadiance);
            command.Parameters.AddWithValue("@DateAndTime", weatherDataFromIOT.DateAndTime);

            try
            {
                var result = await command.ExecuteNonQueryAsync();
                if (result > 0)
                {
                    Debug.WriteLine("weather data successfully inserted");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
            }
        }

    }
}
