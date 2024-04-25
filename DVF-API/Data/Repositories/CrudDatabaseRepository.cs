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
using CoordinateSharp;
using DVF_API.Services.Models;


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




        /// <summary>
        /// returns all weather data within a time period or daily weather data at a specific location
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        public async Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto)
        {
            //  return await GetWeatherDataAsync(searchDto);

            // Create a list to hold the tasks
            //List<Task> tasks = new List<Task>();
            //object lockObject = new object(); // Used to synchronize access to 'weatherData'
            //List<WeatherDataDto> weatherData = new List<WeatherDataDto>();

            //// Create a semaphore to limit the number of concurrent tasks
            //SemaphoreSlim semaphore = new SemaphoreSlim(Environment.ProcessorCount / 2); // Number of logical processors (minus 1 to ensure a bit of space if necessary to allow for other tasks)

            //foreach (string coordinates in searchDto.Coordinates)
            //{
            //    tasks.Add(Task.Run(async () =>
            //    {
            //        await semaphore.WaitAsync(); // Acquire the semaphore
            //        try
            //        {
            //            // Fetch data from the database for the current location
            //          List<WeatherDataDto>   weatherDataForLocation = await FetchWeatherDataForLocationAsync(coordinates, searchDto.FromDate, searchDto.ToDate);

            //            // Add the fetched data to the shared list
            //            lock (lockObject) // Synchronize access to 'weatherData'
            //            {
            //                weatherData.AddRange(weatherDataForLocation);
            //            }
            //        }
            //        finally
            //        {
            //            semaphore.Release(); // Release the semaphore
            //        }
            //    }));
            //}

            //// Wait for all tasks to complete
            //await Task.WhenAll(tasks);

            //MetaDataDto metaDataDto = new MetaDataDto();
            //metaDataDto.WeatherData = weatherData;
            //return metaDataDto;
            return await GetWeatherDataAsync(searchDto);
        }

        private async Task<MetaDataDto> GetWeatherDataAsync(SearchDto searchDto)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                // Split coordinates into latitude and longitude lists
                var latitudes = searchDto.Coordinates.Select(c => c.Split('-')[0]);
                var longitudes = searchDto.Coordinates.Select(c => c.Split('-')[1]);

                // Format the latitude and longitude lists as comma-separated strings
                string latitudeValues = string.Join(",", latitudes.Select(lat => $"'{lat}'"));
                string longitudeValues = string.Join(",", longitudes.Select(lon => $"'{lon}'"));

                string query = "SELECT WD.*, C.CityName, C.PostalCode, L.StreetName, L.StreetNumber, L.Latitude, L.Longitude" +
                    " FROM WeatherDatas WD" +
                    " JOIN Locations L ON WD.LocationId = L.LocationId" +
                    " JOIN Cities C ON L.CityId = C.CityId" +
                    " WHERE WD.DateAndTime >= @FromDate" +
                    " AND WD.DateAndTime <= @ToDate" +
                    $" AND L.Latitude IN ({latitudeValues})" +
                    $" AND L.Longitude IN ({longitudeValues})" + // Added parentheses around longitudeValues
                    " AND WD.IsDeleted = 0";

                await using SqlCommand command = new SqlCommand(query, connection);
                List<WeatherDataDto> weatherData = new List<WeatherDataDto>();
                //while (await reader.ReadAsync())
                //{
                //    Debug.WriteLine(reader["StreetName"].ToString());
                //}


                CultureInfo culture = new CultureInfo("en-US");
                string formattedToDate = searchDto.ToDate.ToString("yyyy-MM-dd", culture);
                string formattedFromDate = searchDto.FromDate.ToString("yyyy-MM-dd", culture);

                command.Parameters.AddWithValue("@FromDate ", formattedFromDate);
                command.Parameters.AddWithValue("@ToDate", formattedToDate);
                //command.Parameters.Add("@Latitude", SqlDbType.VarChar, 255);
                //command.Parameters.Add("@Longitude", SqlDbType.VarChar, 255);



              //  using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
               
                //foreach (string coordinates in searchDto.Coordinates)
                //{
                //    //string latitude = coordinates.Split('-')[0];
                //    //string longitude = coordinates.Split("-")[1];

                //    //command.Parameters["@Latitude"].Value = latitude;
                //    //command.Parameters["@Longitude"].Value = longitude;

                //}

                try
                {
                    using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                    while (await reader.ReadAsync())
                    {
                        WeatherDataDto data = new WeatherDataDto()
                        {
                            Address = $"{reader["StreetName"]} {reader["StreetNumber"]}, {reader["PostalCode"]} {reader["CityName"]}",
                            Latitude = reader["Latitude"].ToString(),
                            Longitude = reader["Longitude"].ToString(),
                            TemperatureC = Convert.ToSingle(reader["TemperatureC"]),
                            WindSpeed = Convert.ToSingle(reader["WindSpeed"]),
                            WindDirection = Convert.ToSingle(reader["WindDirection"]),
                            WindGust = Convert.ToSingle(reader["WindGust"]),
                            RelativeHumidity = Convert.ToSingle(reader["RelativeHumidity"]),
                            Rain = Convert.ToSingle(reader["Rain"]),
                            GlobalTiltedIrRadiance = Convert.ToSingle(reader["GlobalTiltedIrRadiance"]),
                            DateAndTime = Convert.ToDateTime(reader["DateAndTime"]),
                        };

                        weatherData.Add(data);
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred: {ex.Message}");
                    // Ready for logging
                }


                MetaDataDto metaDatamodel = new MetaDataDto()
                {
                    WeatherData = weatherData
                };


                return metaDatamodel;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }


        // Define a method to fetch weather data for a single location asynchronously
        //async Task<List<WeatherDataDto>> FetchWeatherDataForLocationAsync(string coordinates, DateOnly fromDate, DateOnly toDate)
        //{
        //    using SqlConnection connection = new SqlConnection(_connectionString);
        //    await connection.OpenAsync();





        //    string query = "SELECT WD.*, C.CityName, C.PostalCode, L.StreetName, L.StreetNumber, L.Latitude, L.Longitude" +
        //                   " FROM WeatherDatas WD" +
        //                   " JOIN Locations L ON WD.LocationId = L.LocationId" +
        //                   " JOIN Cities C ON L.CityId = C.CityId" +
        //                   " WHERE WD.DateAndTime >= @FromDate" +
        //                   " AND WD.DateAndTime <= @ToDate" +
        //                   " AND L.Latitude = @Latitude" +
        //                   " AND L.Longitude = @Longitude" +
        //                   " AND WD.IsDeleted = 0";

        //    using SqlCommand command = new SqlCommand(query, connection);

        //    CultureInfo culture = new CultureInfo("en-US");
        //    string formattedToDate = toDate.ToString("yyyy-MM-dd", culture);
        //    string formattedFromDate = fromDate.ToString("yyyy-MM-dd", culture);

        //    command.Parameters.AddWithValue("@FromDate", formattedFromDate);
        //    command.Parameters.AddWithValue("@ToDate", formattedToDate);
        //    command.Parameters.AddWithValue("@Latitude", coordinates.Split('-')[0]);
        //    command.Parameters.AddWithValue("@Longitude", coordinates.Split("-")[1]);

        //    using SqlDataReader reader = await command.ExecuteReaderAsync();
        //    List<WeatherDataDto> weatherDatas = new List<WeatherDataDto>();
        //    while (await reader.ReadAsync())
        //    {




        //        // Construct and return a WeatherDataDto object
        //        WeatherDataDto weatherData = new WeatherDataDto()
        //        {
        //            Address = $"{reader["StreetName"]} {reader["StreetNumber"]}, {reader["PostalCode"]} {reader["CityName"]}",
        //            Latitude = reader["Latitude"].ToString(),
        //            Longitude = reader["Longitude"].ToString(),
        //            TemperatureC = Convert.ToSingle(reader["TemperatureC"]),
        //            WindSpeed = Convert.ToSingle(reader["WindSpeed"]),
        //            WindDirection = Convert.ToSingle(reader["WindDirection"]),
        //            WindGust = Convert.ToSingle(reader["WindGust"]),
        //            RelativeHumidity = Convert.ToSingle(reader["RelativeHumidity"]),
        //            Rain = Convert.ToSingle(reader["Rain"]),
        //            GlobalTiltedIrRadiance = Convert.ToSingle(reader["GlobalTiltedIrRadiance"]),
        //            DateAndTime = Convert.ToDateTime(reader["DateAndTime"]),
        //        };
        //        weatherDatas.Add(weatherData);
        //    }

        //    return weatherDatas;
        //}

        /// <summary>
        /// Deletes all weather data until specific date 
        /// </summary>
        /// <param name="deleteWeatherDataBeforeThisDate"></param>
        /// <returns></returns>
        public async Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            await RemoveOldData(deleteWeatherDataBeforeThisDate);
        }
        private async Task RemoveOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            try
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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }



        /// <summary>
        /// Restores all weather data in the database
        /// </summary>
        /// <returns></returns>
        public async Task RestoreAllData()
        {
            await RestoreData();
        }
        private async Task RestoreData()
        {
            try
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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }



        /// <summary>
        /// extracts locations based on indexes
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns></returns>
        public async Task<List<string>> FetchLocationCoordinates(int fromIndex, int toIndex)
        {
            return await GetLocationCoordinates(fromIndex, toIndex);
        }
        private async Task<List<string>> GetLocationCoordinates(int fromIndex, int toIndex)
        {
            try
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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }



        /// <summary>
        /// returns the total number of locations in the database
        /// </summary>
        /// <returns></returns>
        public async Task<int> FetchLocationCount()
        {

            return await GetLocationCount();
        }
        private async Task<int> GetLocationCount()
        {
            try
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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }


        /// <summary>
        /// returns a list of addresses based on partial search
        /// </summary>
        /// <param name="partialAddress"></param>
        /// <returns></returns>
        public async Task<List<string>> FetchMatchingAddresses(string partialAddress)
        {
            return await GethMatchingAddresses(partialAddress);
        }
        private async Task<List<string>> GethMatchingAddresses(string partialAddress)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT Locations.StreetName, Locations.StreetNumber, Cities.PostalCode, Cities.CityName" +
                    " FROM Locations JOIN Cities ON Locations.CityId = Cities.CityId" +
                    " WHERE(Locations.StreetName + ' ' + Locations.StreetNumber) LIKE @searchCriteria +'%'" +
                    " OR Cities.PostalCode LIKE @searchCriteria + '%'" +
                    " OR Cities.CityName LIKE @searchCriteria + '%'";


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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }



        /// <summary>
        /// adding weather data to database
        /// </summary>
        /// <param name="weatherDataFromIOT"></param>
        /// <returns></returns>
        public async Task InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            await InsertWeatherData(weatherDataFromIOT);
        }
        private async Task InsertWeatherData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            try
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
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }
        }


        /// <summary>
        /// adding addresses from latitude and longitude to binary data
        /// </summary>
        /// <param name="BinaryData"></param>
        /// <returns></returns>
        public async Task<List<BinaryDataFromFileDto>> FetchAddressByCoordinates(List<BinaryDataFromFileDto> BinaryData)
        {
            return await GetAddressByCoordinates(BinaryData);
        }
        private async Task<List<BinaryDataFromFileDto>> GetAddressByCoordinates(List<BinaryDataFromFileDto> BinaryData)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT Locations.StreetName, Locations.StreetNumber, Cities.PostalCode, Cities.CityName" +
                    " FROM Locations JOIN Cities ON Locations.CityId = Cities.CityId" +
                    " WHERE Locations.Latitude = @latitude AND Locations.Longitude = @longitude";


                await using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add("@Latitude", SqlDbType.VarChar, 255);
                command.Parameters.Add("@Longitude", SqlDbType.VarChar, 255);

                foreach (var data in BinaryData)
                {
                    string latitude = data.Coordinates.Split('-')[0];
                    string longitude = data.Coordinates.Split("-")[1];
                    command.Parameters["@Latitude"].Value = latitude;
                    command.Parameters["@Longitude"].Value = longitude;

                    try
                    {
                        var result = await command.ExecuteReaderAsync();
                        while (await result.ReadAsync())
                        {
                            data.Address = $"{result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}";
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"An error occurred: {ex.Message}");
                        // Ready for logging
                    }
                }

                return BinaryData;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }


        }

    }
}
