using DVF_API.Data.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;


namespace DVF_API.Data.Repositories
{

    /// <summary>
    /// This class is responsible for CRUD operations on the database
    /// </summary>
    public class CrudDatabaseRepository : ICrudDatabaseRepository, ILocationRepository
    {

        #region Fields
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        #endregion




        #region Constructor
        public CrudDatabaseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb");
        }
        #endregion




        /// <summary>
        /// Calls the private method to fetch weather data from the database
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        public async Task<MetaDataDto> FetchWeatherDataAsync(SearchDto searchDto)
        {
            return await GetWeatherDataAsync(searchDto);
        }




        /// <summary>
        /// Calls the private method to delete all data in the database
        /// </summary>
        /// <param name="deleteWeatherDataBeforeThisDate"></param>
        /// <returns></returns>
        public async Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            await RemoveOldData(deleteWeatherDataBeforeThisDate);
        }




        /// <summary>
        /// Calls the private method to restore all data in the database
        /// </summary>
        /// <returns>A task</returns>
        public async Task RestoreAllData()
        {
            await RestoreData();
        }




        /// <summary>
        /// Calls the private method to fetch all coordinates from the database
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns></returns>
        public async Task<Dictionary<long, string>> FetchLocationCoordinates(int fromIndex, int toIndex)
        {
            return await GetLocationCoordinates(fromIndex, toIndex);
        }




        /// <summary>
        /// Calls the private method to fetch all addresses from the database
        /// </summary>
        /// <returns></returns>
        public async Task<int> FetchLocationCount()
        {
            return await GetLocationCount();
        }




        /// <summary>
        /// Calls the private method to fetch all addresses from the database
        /// </summary>
        /// <param name="partialAddress"></param>
        /// <returns>A list of addresses</returns>
        public async Task<List<string>> FetchMatchingAddresses(string partialAddress)
        {
            return await GethMatchingAddresses(partialAddress);
        }




        /// <summary>
        /// Calls the private method to fetch all addresses from the database based on the coordinates
        /// </summary>
        /// <param name="BinaryData"></param>
        /// <returns>A list of binary data from file dtos</returns>
        public async Task<List<BinaryDataFromFileDto>> FetchAddressByCoordinates(SearchDto searchDto)
        {
            return await GetAddressByCoordinates(searchDto);
        }




        /// <summary>
        /// Calls the private method to fetch all addresses from the database
        /// </summary>
        /// <returns>A Dictionary with the location id as the key and the address as the value</returns>
        public async Task<Dictionary<long, LocationDto>> GetAllLocationCoordinates()
        {
            return await FetchAllLocationCoordinates();
        }




        /// <summary>
        /// Fetches the weather data from the database based on the search dto
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns>A meta data dto</returns>
        private async Task<MetaDataDto> GetWeatherDataAsync(SearchDto searchDto)
        {
            try
            {
                //List<BinaryDataFromFileDto> Locations = await FetchAddressByCoordinates(searchDto);
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = "";
                if (searchDto.Coordinates.Count > 0)
                {

                    var latitudes = searchDto.Coordinates.Select(c => c.Split('-')[0]);
                    var longitudes = searchDto.Coordinates.Select(c => c.Split('-')[1]);

                    string latitudeValues = string.Join(",", latitudes.Select(lat => $"'{lat}'"));
                    string longitudeValues = string.Join(",", longitudes.Select(lon => $"'{lon}'"));

                    query = "SELECT WD.*, C.CityName, C.PostalCode, L.StreetName, L.StreetNumber, L.Latitude, L.Longitude" +
                  " FROM WeatherDatas WD" +
                  " JOIN Locations L ON WD.LocationId = L.LocationId" +
                  " JOIN Cities C ON L.CityId = C.CityId" +
                  " WHERE WD.DateAndTime >= @FromDate" +
                  " AND WD.DateAndTime < @ToDate" +
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
                  " AND WD.DateAndTime < @ToDate" +
                  " AND WD.IsDeleted = 0";
                }

                List<WeatherDataDto> weatherData = new List<WeatherDataDto>();
                await using SqlCommand command = new SqlCommand(query, connection);

                CultureInfo culture = new CultureInfo("en-US");
                DateOnly toDatePlusOne = searchDto.ToDate.AddDays(1);
                string formattedToDate = toDatePlusOne.ToString("yyyy-MM-dd", culture);
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
                    // Ready for logging $"An error occurred: {ex.Message}"
                }

                MetaDataDto metaDatamodel = new MetaDataDto()
                {
                    WeatherData = weatherData
                };

                return metaDatamodel;
            }
            catch (Exception e)
            {
                // Ready for logging $"GetWeatherDataAsync failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Deletes all weather data until specific date
        /// </summary>
        /// <param name="deleteWeatherDataBeforeThisDate"></param>
        /// <returns>A task</returns>
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
                }
                catch (Exception ex)
                {
                    // Ready for logging $"An error occurred: {ex.Message}"
                }
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Restores all weather data in the database
        /// </summary>
        /// <returns>A task</returns>
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
                }
                catch (Exception ex)
                {
                    // Ready for logging $"An error occurred: {ex.Message}"
                }
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Gets the location coordinates based on the indexes
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns>A dictionary with the location id as the key and the coordinates as the value</returns>
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
                    // Ready for logging $"An error occurred: {ex.Message}"
                }
                return null;
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Fetches the number of locations in the database
        /// </summary>
        /// <returns>An integer representing the number of locations</returns>
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
                    // Ready for logging $"An error occurred: {ex.Message}"
                    return 0;
                }
            }
            catch (Exception)
            {
                // Ready for logging $"GetLocationCount failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Fetches the addresses that match the partial address
        /// </summary>
        /// <param name="partialAddress"></param>
        /// <returns>A list of addresses</returns>
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
                        string combinedAddress = $"{result["LocationId"]}: {result["StreetName"]} {result["StreetNumber"]}, {result["PostalCode"]} {result["CityName"]}";

                        addresses.Add(combinedAddress);
                    }
                    return addresses;
                }
                catch (Exception ex)
                {
                    // Ready for logging $"An error occurred: {ex.Message}"
                    throw;
                }
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Fetches the addresses based on the coordinates
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns>A list of binary data from file dtos</returns>
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
                            // Ready for logging $"An error occurred: {ex.Message}"
                        }
                    }
                }
                return binaryDataFromFileDtos;
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }




        /// <summary>
        /// Fetches all location coordinates from the database
        /// </summary>
        /// <returns>A dictionary with the location id as the key and the location dto as the value</returns>
        private async Task<Dictionary<long, LocationDto>> FetchAllLocationCoordinates()
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM Locations l JOIN Cities c On l.CityId =c.CityId";
                await using SqlCommand command = new SqlCommand(query, connection);

                Dictionary<long, LocationDto> locationDtos = new Dictionary<long, LocationDto>();
                try
                {
                    var result = await command.ExecuteReaderAsync();
                    while (await result.ReadAsync())
                    {
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
                        locationDtos.Add(id, locationDto);
                    }
                    return locationDtos;
                }
                catch (Exception ex)
                {
                    // Ready for logging $"An error occurred: {ex.Message}"
                    throw;
                }
            }
            catch (Exception e)
            {
                // Ready for logging $"method failed: {e.Message}"
                throw;
            }
        }
    }
}
