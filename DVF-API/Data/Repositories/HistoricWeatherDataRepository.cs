﻿using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace DVF_API.Data.Repositories
{
    public class HistoricWeatherDataRepository : IHistoricWeatherDataRepository
    {
        private readonly IUtilityManager _utilityManager;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;


        #region Constructors
        public HistoricWeatherDataRepository(IConfiguration configuration, IUtilityManager utilityManager)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb")!;
            _utilityManager = utilityManager;
        }
        #endregion




        public async Task SaveDataToFileAsync(List<SaveToStorageDto> saveToStorageDtoList, string baseFolder)
        {
            await SaveDataAsBinaryFilesAsync(saveToStorageDtoList, baseFolder);
        }




        public async Task SaveDataToDatabaseAsync(List<SaveToStorageDto> saveToStorageDtoList)
        {
            await InsertWeatherDataToDatabaseAsync(saveToStorageDtoList, _connectionString);
        }




        public async Task SaveCitiesToDBAsync(List<City> cities)
        {
            await InsertCitiesToDB(cities);
        }





        public async Task SaveLocationsToDBAsync(List<LocationDto> locations)
        {
            await InsertLocationsToDB(locations);
        }




        public async Task SaveCoordinatesToDBAsync(List<string> coordinates)
        {
            await InsertCordinatesToDB(coordinates);
        }




        private async Task InsertCitiesToDB(List<City> cities)
        {
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO Cities (PostalCode, CityName) VALUES (@PostalCode, @CityName)";

                await using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.Add("@PostalCode", SqlDbType.VarChar, 255);
                command.Parameters.Add("@CityName", SqlDbType.VarChar, 255);

                foreach (City city in cities)
                {
                    command.Parameters["@PostalCode"].Value = city.PostalCode;
                    command.Parameters["@CityName"].Value = city.CityName;

                    int result = await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                // Ready for logging
            }
        }




        private async Task InsertLocationsToDB(List<LocationDto> locations)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    Dictionary<string, int> cityIds = new Dictionary<string, int>();
                    using (SqlCommand command = new SqlCommand("SELECT CityId, PostalCode FROM Cities", connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cityIds[reader["PostalCode"].ToString()] = (int)reader["CityId"];
                            }
                        }
                    }

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("Latitude", typeof(string));
                    dataTable.Columns.Add("Longitude", typeof(string));
                    dataTable.Columns.Add("StreetName", typeof(string));
                    dataTable.Columns.Add("StreetNumber", typeof(string));
                    dataTable.Columns.Add("CityId", typeof(int));

                    foreach (var location in locations)
                    {
                        if (cityIds.TryGetValue(location.PostalCode, out int cityId))
                        {
                            dataTable.Rows.Add(location.Latitude, location.Longitude, location.StreetName, location.StreetNumber, cityId);
                        }
                        else
                        {
                            Debug.WriteLine($"No CityId found for PostalCode: {location.PostalCode}");
                        }
                    }

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = "Locations";

                        bulkCopy.ColumnMappings.Add("Latitude", "Latitude");
                        bulkCopy.ColumnMappings.Add("Longitude", "Longitude");
                        bulkCopy.ColumnMappings.Add("StreetName", "StreetName");
                        bulkCopy.ColumnMappings.Add("StreetNumber", "StreetNumber");
                        bulkCopy.ColumnMappings.Add("CityId", "CityId");

                        await bulkCopy.WriteToServerAsync(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ready for logging
            }
        }




        private async Task InsertCordinatesToDB(List<string> coordinates)
        {
            try
            {
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("Coordinate", typeof(string));

                // Prepare the DataTable with all coordinates
                foreach (string coordinate in coordinates)
                {
                    dataTable.Rows.Add(coordinate);
                }

                Debug.WriteLine($"Rows to insert: {dataTable.Rows.Count}");  // Check how many rows are prepared

                // Bulk copy to database
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_connectionString))
                {
                    bulkCopy.DestinationTableName = "Coordinates";
                    bulkCopy.ColumnMappings.Add("Coordinate", "Coordinate");  // Ensure correct mapping

                    await bulkCopy.WriteToServerAsync(dataTable);
                    Debug.WriteLine("Data inserted successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                throw;  // Rethrow to handle higher up or log with more details
            }
        }





        private async Task SaveDataAsBinaryFilesAsync(List<SaveToStorageDto> dataToSave, string baseDirectory)
        {
            try
            {
                ConcurrentBag<HistoricWeatherDataToFileDto> historicWeatherDataToFileDtos = new ConcurrentBag<HistoricWeatherDataToFileDto>();

                Parallel.ForEach(dataToSave, data =>
                {
                    Parallel.ForEach(data.HistoricWeatherData.Hourly.Time, (time, _, index) =>
                    {
                        HistoricWeatherDataToFileDto historicWeatherDataToFileDto = new HistoricWeatherDataToFileDto
                        {
                            Latitude = ConvertCoordinate(data.Latitude),
                            Longitude = ConvertCoordinate(data.Longitude),
                            Time = ConvertDateTimeToFloatInternal(time),
                            Temperature_2m = data.HistoricWeatherData.Hourly.Temperature_2m[index],
                            Relative_Humidity_2m = data.HistoricWeatherData.Hourly.Relative_Humidity_2m[index],
                            Rain = data.HistoricWeatherData.Hourly.Rain[index],
                            Wind_Speed_10m = data.HistoricWeatherData.Hourly.Wind_Speed_10m[index],
                            Wind_Direction_10m = data.HistoricWeatherData.Hourly.Wind_Direction_10m[index],
                            Wind_Gusts_10m = data.HistoricWeatherData.Hourly.Wind_Gusts_10m[index],
                            Global_Tilted_Irradiance_Instant = data.HistoricWeatherData.Hourly.Global_Tilted_Irradiance_Instant[index]
                        };
                        historicWeatherDataToFileDtos.Add(historicWeatherDataToFileDto);
                    });
                });

                var groupedData = historicWeatherDataToFileDtos.GroupBy(dto => MixedYearDateTimeSplitter(dto.Time));
                historicWeatherDataToFileDtos = new ConcurrentBag<HistoricWeatherDataToFileDto>();

                foreach (var group in groupedData)
                {
                    string date = group.Key[0].ToString()!; // Full date YYYYMMDD
                    var year = date.Substring(0, 4);
                    var monthDay = date.Substring(4, 4);
                    var yearDirectory = Path.Combine(baseDirectory, year);
                    Directory.CreateDirectory(yearDirectory); 
                    var fileName = Path.Combine(yearDirectory, $"{monthDay}.bin");

                    using (var fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        using (var binaryWriter = new BinaryWriter(fileStream))
                        {
                            foreach (var groupItem in group)
                            {
                                binaryWriter.Write(groupItem.Latitude);
                                binaryWriter.Write(groupItem.Longitude);
                                binaryWriter.Write((float)MixedYearDateTimeSplitter(groupItem.Time)[1]);
                                binaryWriter.Write(groupItem.Temperature_2m);
                                binaryWriter.Write(groupItem.Relative_Humidity_2m);
                                binaryWriter.Write(groupItem.Rain);
                                binaryWriter.Write(groupItem.Wind_Speed_10m);
                                binaryWriter.Write(groupItem.Wind_Direction_10m);
                                binaryWriter.Write(groupItem.Wind_Gusts_10m);
                                binaryWriter.Write(groupItem.Global_Tilted_Irradiance_Instant);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }



        private double ConvertDateTimeToFloatInternal(string time)
        {
            DateTime parsedDateTime = DateTime.Parse(time);
            return double.Parse(parsedDateTime.ToString("yyyyMMddHHmm"));
        }


        private float ConvertCoordinate(string coordinate)
        {
            var normalized = coordinate.Replace(',', '.');
            return float.Parse(normalized, CultureInfo.InvariantCulture);
        }


        private object[] MixedYearDateTimeSplitter(double time)
        {
            object[] result = new object[2]; // Change to 2 elements for Year-Month-Day and Hour-Minute
            string timeString = time.ToString("000000000000");

            // Extract year, month, and day
            result[0] = timeString.Substring(0, 8); // Returns YYYYMMDD

            // Extract HHmm as float
            result[1] = float.Parse(timeString.Substring(8, 4)); // Returns HHmm

            return result;
        }

























        /// <summary>
        /// Inserts weather data into the database, using a MERGE operation to speed up the process.
        /// </summary>
        /// <param name="saveToStorageDtoDataList"></param>
        /// <param name="connectionString"></param>
        /// <returns>Returns a Task.</returns>
        private async Task InsertWeatherDataToDatabaseAsync(List<SaveToStorageDto> saveToStorageDtoDataList, string connectionString)
        {
            try
            {
                var locationDictionary = new Dictionary<string, int>();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand("", connection);

                    // Cache location IDs
                    command.CommandText = "SELECT LocationId, Latitude, Longitude FROM Locations";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string key = $"{reader["Latitude"]},{reader["Longitude"]}";
                            if (!locationDictionary.ContainsKey(key))
                            {
                                locationDictionary[key] = (int)reader["LocationId"];
                            }
                        }
                    }

                    // Setup temporary table
                    string tempTableName = "#tempWeatherData";
                    command.CommandText = $@"
                    CREATE TABLE {tempTableName} (
                    TemperatureC DECIMAL(10, 2),
                    WindSpeed DECIMAL(10, 2),
                    WindDirection DECIMAL(10, 2),
                    WindGust DECIMAL(10, 2),
                    RelativeHumidity DECIMAL(10, 2),
                    Rain DECIMAL(10, 2),
                    GlobalTiltedIrRadiance DECIMAL(10, 2),
                    DateAndTime DATETIME,
                    LocationId INT,
                    IsDeleted BIT
                    );";

                    command.ExecuteNonQuery();

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("TemperatureC", typeof(float));
                    dataTable.Columns.Add("WindSpeed", typeof(float));
                    dataTable.Columns.Add("WindDirection", typeof(float));
                    dataTable.Columns.Add("WindGust", typeof(float));
                    dataTable.Columns.Add("RelativeHumidity", typeof(float));
                    dataTable.Columns.Add("Rain", typeof(float));
                    dataTable.Columns.Add("GlobalTiltedIrRadiance", typeof(float));
                    dataTable.Columns.Add("DateAndTime", typeof(DateTime));
                    dataTable.Columns.Add("LocationId", typeof(int));
                    dataTable.Columns.Add("IsDeleted", typeof(bool));

                    foreach (var data in saveToStorageDtoDataList)
                    {
                        string locationKey = $"{data.Latitude},{data.Longitude}";
                        if (locationDictionary.TryGetValue(locationKey, out int locationId))
                        {
                            var weatherData = data.HistoricWeatherData.Hourly;
                            for (int i = 0; i < weatherData.Time.Length; i++)
                            {

                                DateTime parsedDate = DateTime.ParseExact(weatherData.Time[i], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                                dataTable.Rows.Add(
                                    Math.Round(weatherData.Temperature_2m[i], 2),
                                    Math.Round(weatherData.Wind_Speed_10m[i], 2),
                                    Math.Round(weatherData.Wind_Direction_10m[i], 2),
                                    Math.Round(weatherData.Wind_Gusts_10m[i], 2),
                                    Math.Round(weatherData.Relative_Humidity_2m[i], 2),
                                    Math.Round(weatherData.Rain[i], 2),
                                    Math.Round(weatherData.Global_Tilted_Irradiance_Instant[i], 2),
                                    parsedDate,
                                    locationId,
                                    false
                                );
                            }
                        }
                        else
                        {
                            Debug.WriteLine("LocationId ikke fundet for nøglen: " + locationKey);
                        }
                    }

                    if (dataTable.Rows.Count > 0)
                    {
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                        {
                            bulkCopy.DestinationTableName = tempTableName;
                            await bulkCopy.WriteToServerAsync(dataTable);

                            // MERGE operation
                            command.CommandText = $@"
                            MERGE INTO WeatherDatas AS target
                            USING {tempTableName} AS source
                            ON target.DateAndTime = source.DateAndTime AND target.LocationId = source.LocationId
                            WHEN NOT MATCHED THEN
                            INSERT (TemperatureC, WindSpeed, WindDirection, WindGust, RelativeHumidity, Rain, GlobalTiltedIrRadiance, DateAndTime, LocationId, IsDeleted)
                            VALUES (source.TemperatureC, source.WindSpeed, source.WindDirection, source.WindGust, source.RelativeHumidity, source.Rain, source.GlobalTiltedIrRadiance, source.DateAndTime, source.LocationId, source.IsDeleted);";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Ingen data at indsætte.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fejl under databaseoperation: " + ex.Message);
                throw;
            }
        }





    }
}
