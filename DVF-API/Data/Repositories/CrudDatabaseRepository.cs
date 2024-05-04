using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;


namespace DVF_API.Data.Repositories
{
    public class CrudDatabaseRepository : ICrudDatabaseRepository, ILocationRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public CrudDatabaseRepository(IConfiguration configuration, IUtilityManager utilityManager, ISolarPositionManager solarPositionManager)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb");
        }




        /// <summary>
        /// returns all weather data within a time period or daily weather data at a specific location
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        public async Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto)
        {
            return await GetWeatherDataAsync(searchDto);
        }

        private async Task<MetaDataDto> GetWeatherDataAsync(SearchDto searchDto)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = "";
                if(searchDto.Coordinates.Count > 0)
                {

                    // Split coordinates into latitude and longitude lists
                    var latitudes = searchDto.Coordinates.Select(c => c.Split('-')[0]);
                    var longitudes = searchDto.Coordinates.Select(c => c.Split('-')[1]);

                    // Format the latitude and longitude lists as comma-separated strings
                    string latitudeValues = string.Join(",", latitudes.Select(lat => $"'{lat}'"));
                    string longitudeValues = string.Join(",", longitudes.Select(lon => $"'{lon}'"));

                    query = "SELECT WD.*, C.CityName, C.PostalCode, L.StreetName, L.StreetNumber, L.Latitude, L.Longitude" +
                  " FROM WeatherDatas WD" +
                  " JOIN Locations L ON WD.LocationId = L.LocationId" +
                  " JOIN Cities C ON L.CityId = C.CityId" +
                  " WHERE WD.DateAndTime >= @FromDate" +
                  " AND WD.DateAndTime <= @ToDate" +
                  $" AND L.Latitude IN ({latitudeValues})" +
                  $" AND L.Longitude IN ({longitudeValues})" +
                  " AND WD.IsDeleted = 0";
                }
                else
                {
                    query = "SELECT WD.*, C.CityName, C.PostalCode, L.StreetName, L.StreetNumber, L.Latitude, L.Longitude" +
                  " FROM WeatherDatas WD" +
                  " JOIN Locations L ON WD.LocationId = L.LocationId" +
                  " JOIN Cities C ON L.CityId = C.CityId" +
                  " WHERE WD.DateAndTime >= @FromDate" +
                  " AND WD.DateAndTime <= @ToDate" +
                  " AND WD.IsDeleted = 0";
                }


                

               

                await using SqlCommand command = new SqlCommand(query, connection);
                List<WeatherDataDto> weatherData = new List<WeatherDataDto>();

                CultureInfo culture = new CultureInfo("en-US");
                string formattedToDate = searchDto.ToDate.ToString("yyyy-MM-dd", culture);
                string formattedFromDate = searchDto.FromDate.ToString("yyyy-MM-dd", culture);

                command.Parameters.AddWithValue("@FromDate ", formattedFromDate);
                command.Parameters.AddWithValue("@ToDate", formattedToDate);

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
        public async Task<Dictionary<long, string>> FetchLocationCoordinates(int fromIndex, int toIndex)
        {
            return await GetLocationCoordinates(fromIndex, toIndex);
        }
        private async Task<Dictionary<long, string>> GetLocationCoordinates(int fromIndex, int toIndex)
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
                    Dictionary<long, string> coordinates = new Dictionary<long, string>();

                    while (await result.ReadAsync())
                    {
                        string latitude = result["Latitude"].ToString();
                        string longitude = result["Longitude"].ToString();
                        int idIndex = result.GetOrdinal("locationId");
                        int id = result.GetInt32(idIndex);


                        string coordinate = $"{latitude}-{longitude}";
                        coordinates.Add(id, coordinate);
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

                string query = "SELECT Locations.StreetName, Locations.StreetNumber, Locations.LocationId, Cities.PostalCode, Cities.CityName" +
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
                        string combinedAddress = $"{result["LocationId"]}: {result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}";

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
        public async Task<List<BinaryDataFromFileDto>> FetchAddressByCoordinates(SearchDto searchDto)
        {
            return await GetAddressByCoordinates(searchDto);
        }
        private async Task<List<BinaryDataFromFileDto>> GetAddressByCoordinates(SearchDto searchDto)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = "";
                List<BinaryDataFromFileDto> binaryDataFromFileDtos = new List<BinaryDataFromFileDto>();
                if (searchDto.Coordinates.Count == 0)
                {
                    query = "SELECT Locations.LocationId, Locations.StreetName, Locations.StreetNumber, Cities.PostalCode, Cities.CityName" +
                   " FROM Locations JOIN Cities ON Locations.CityId = Cities.CityId";


                    await using SqlCommand command = new SqlCommand(query, connection);

                    var result = await command.ExecuteReaderAsync();
                    while (await result.ReadAsync())
                    {
                        BinaryDataFromFileDto binaryDataFromFileDto = new BinaryDataFromFileDto()
                        {
                            Address = $"{result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}",
                            LocationId = result.GetInt32(result.GetOrdinal("LocationId")),
                        };

                        binaryDataFromFileDtos.Add(binaryDataFromFileDto);
                    }
                }
                else
                {
                    query = "SELECT Locations.LocationId, Locations.StreetName, Locations.StreetNumber, Cities.PostalCode, Cities.CityName" +
                   " FROM Locations JOIN Cities ON Locations.CityId = Cities.CityId" +
                   " WHERE Locations.Latitude = @latitude AND Locations.Longitude = @longitude";
                    await using SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.Add("@Latitude", SqlDbType.VarChar, 255);
                    command.Parameters.Add("@Longitude", SqlDbType.VarChar, 255);
                    foreach (var data in searchDto.Coordinates)
                    {
                        string latitude = data.Split('-')[0];
                        string longitude = data.Split("-")[1];
                        command.Parameters["@Latitude"].Value = latitude;
                        command.Parameters["@Longitude"].Value = longitude;

                        try
                        {
                            var result = await command.ExecuteReaderAsync();
                            while (await result.ReadAsync())
                            {
                                BinaryDataFromFileDto binaryDataFromFileDto = new BinaryDataFromFileDto()
                                {
                                    Address = $"{result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}",
                                    LocationId = result.GetInt32(result.GetOrdinal("LocationId")),
                                };

                                binaryDataFromFileDtos.Add(binaryDataFromFileDto);
                            }
                            result.CloseAsync();

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"An error occurred: {ex.Message}");
                            // Ready for logging
                        }
                    }

                }






                return binaryDataFromFileDtos;

            }
            catch (Exception e)
            {
                Debug.WriteLine($"method failed: {e.Message}");
                throw;
            }


        }

        public async Task<Dictionary<long,LocationDto>> GetAllLocationCoordinates()
        {
            return await FetchAllLocationCoordinates();
        }


        private async Task<Dictionary<long, LocationDto>> FetchAllLocationCoordinates()
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM Locations l JOIN Cities c On l.CityId =c.CityId";
                await using SqlCommand command = new SqlCommand(query, connection);

                //command.Parameters.AddWithValue("@fromIndex", fromIndex);
                //command.Parameters.AddWithValue("@toIndex", toIndex);
               Dictionary<long,LocationDto> locationDtos = new Dictionary<long,LocationDto>();
                try
                {
                  
                    var result = await command.ExecuteReaderAsync();
                    while (await result.ReadAsync())
                    {

                        // string coordinate = $"{latitude}-{longitude}";
                        long id = result.GetInt32(result.GetOrdinal("LocationId"));
                        LocationDto locationDto = new LocationDto()
                        {


                            Latitude = result["Latitude"].ToString(),
                            Longitude = result["Longitude"].ToString(),
                            StreetName = result["StreetName"].ToString(),
                            StreetNumber = result["StreetNumber"].ToString(),
                            PostalCode = result["PostalCode"].ToString(),
                            CityName = result["CityName"].ToString()

                        };

                        locationDtos.Add(id,locationDto);

                    }
                    return locationDtos;
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
               
            }
            return null;
        }
    }
}
