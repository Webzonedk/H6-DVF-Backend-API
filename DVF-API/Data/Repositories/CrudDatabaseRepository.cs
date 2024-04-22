using DVF_API.Data.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;
using System;
using DVF_API.Data.Models;


namespace DVF_API.Data.Repositories
{
    public class CrudDatabaseRepository : IDatabaseRepository, ILocationRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public CrudDatabaseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb");
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


        public MetaDataDto FetchWeatherData(SearchDto searchDto)
        {
            //// Parse and filter by coordinates
            //var coordinates = searchDto.Coordinates
            //    .Select(coord => {
            //        var parts = coord.Split('-');
            //        return new { Latitude = double.Parse(parts[0], CultureInfo.InvariantCulture), Longitude = double.Parse(parts[1], CultureInfo.InvariantCulture) };
            //    }).ToList();

            //// Query the database
            //var query = _dvfDbContext.WeatherDatas
            //    .Include(wd => wd.Location)
            //        .ThenInclude(l => l.City)
            //    .Where(wd => coordinates.Any(c => c.Latitude == wd.Location.Latitude && c.Longitude == wd.Location.Longitude))
            //    .Where(wd => wd.DateAndTime >= searchDto.FromDate && wd.DateAndTime <= searchDto.ToDate);

            //// Project to WeatherDataDto (Assuming you have a mapper or manually map the fields)
            //var weatherDataDtos = query.Select(wd => new WeatherDataDto
            //{
            //    Latitude = (float)wd.Location.Latitude,
            //    Longitude = (float)wd.Location.Longitude,
            //    TemperatureC = wd.TemperatureC,
            //    WindSpeed = wd.WindSpeed,
            //    WindDirection = wd.WindDirection,
            //    WindGust = wd.WindGust,
            //    RelativeHumidity = wd.RelativeHumidity,
            //    Rain = wd.Rain,
            //    GlobalTiltedIrRadiance = wd.GlobalTiltedIrRadiance,
            //    DateAndTime = wd.DateAndTime,
            //    // Additional mappings can be added here
            //}).ToList();

            // Populate the MetaDataDto
            return new MetaDataDto
            {
                //WeatherData = weatherDataDtos,
            };
        }

        public void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            throw new NotImplementedException();
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
                    // Read Latitude and Longitude values from the result set
                    string latitude = result["Latitude"].ToString();
                    string longitude = result["Longitude"].ToString();

                    // Combine Latitude and Longitude into a single string
                    string coordinate = $"{latitude}-{longitude}";

                    // Add the combined coordinate to the list
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

            string query = "SELECT Locations.StreetName, Locations.StreetNumber, Cities.PostalCode,Cities.CityName FROM Locations " +
                "JOIN Cities ON Locations.CityId = Cities.CityId" +
                "WHERE(Locations.StreetName + ' ' + Locations.StreetNumber LIKE @searchCriteria " +
                "OR Cities.PostalCode LIKE @searchCriteria OR Cities.CityName LIKE @searchCriteria)";

            
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

        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {


            try
            {

            }
            catch
            {

            }
        }

        public void RestoreAllData()
        {
            throw new NotImplementedException();
        }

    }
}
