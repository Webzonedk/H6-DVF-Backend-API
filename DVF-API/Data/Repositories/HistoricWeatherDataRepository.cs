using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;

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




        public async Task SaveDataToFileAsync(string fileName, byte[] byteArrayToSaveToFile)
        {
            await SaveDataAsBinaryFilesAsync(fileName, byteArrayToSaveToFile);

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







        private async Task SaveDataAsBinaryFilesAsync(string fileName, byte[] byteArrayToSaveToFile)
        {
            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await fileStream.WriteAsync(byteArrayToSaveToFile, 0, byteArrayToSaveToFile.Length);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Access denied: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IO error: {ex.Message}");
                if (ex is PathTooLongException)
                {
                    Debug.WriteLine("The specified path, file name, or both exceed the system-defined maximum length.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"A File writer error occurred: {ex.Message}");
            }
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
                            var weatherData = data.HistoricWeatherData;
                            for (int i = 0; i < weatherData.Time.Length; i++)
                            {

                                DateTime parsedDate = DateTime.ParseExact(weatherData.Hourly.Time[i], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                                dataTable.Rows.Add(
                                    Math.Round(weatherData.teTemperature_2m[i], 2),
                                    Math.Round(weatherData.Hourly.Wind_Speed_10m[i], 2),
                                    Math.Round(weatherData.Hourly.Wind_Direction_10m[i], 2),
                                    Math.Round(weatherData.Hourly.Wind_Gusts_10m[i], 2),
                                    Math.Round(weatherData.Hourly.Relative_Humidity_2m[i], 2),
                                    Math.Round(weatherData.Hourly.Rain[i], 2),
                                    Math.Round(weatherData.Hourly.Global_Tilted_Irradiance_Instant[i], 2),
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
                        int batchSize = 50000; // Kan justeres efter performance tests
                        for (int i = 0; i < dataTable.Rows.Count; i += batchSize)
                        {
                            Debug.WriteLine($"Indsætter række {i} til {i + batchSize} af {dataTable.Rows.Count}");
                            var batchTable = new DataTable();
                            batchTable = dataTable.Clone(); // Kopier struktur
                            for (int j = 0; j < batchSize && (i + j) < dataTable.Rows.Count; j++)
                            {
                                batchTable.ImportRow(dataTable.Rows[i + j]);
                            }

                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                            {
                                bulkCopy.DestinationTableName = tempTableName;
                                bulkCopy.BatchSize = batchSize;
                                await bulkCopy.WriteToServerAsync(batchTable);

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







        ///// <summary>
        ///// Inserts weather data into the database, using a MERGE operation to speed up the process.
        ///// </summary>
        ///// <param name="saveToStorageDtoDataList"></param>
        ///// <param name="connectionString"></param>
        ///// <returns>Returns a Task.</returns>
        //private async Task InsertWeatherDataToDatabaseAsync(List<SaveToStorageDto> saveToStorageDtoDataList, string connectionString)
        //{
        //    try
        //    {
        //        var locationDictionary = new Dictionary<string, int>();
        //        using (var connection = new SqlConnection(connectionString))
        //        {
        //            await connection.OpenAsync();
        //            SqlCommand command = new SqlCommand("", connection);

        //            // Cache location IDs
        //            command.CommandText = "SELECT LocationId, Latitude, Longitude FROM Locations";
        //            using (var reader = await command.ExecuteReaderAsync())
        //            {
        //                while (await reader.ReadAsync())
        //                {
        //                    string key = $"{reader["Latitude"]},{reader["Longitude"]}";
        //                    if (!locationDictionary.ContainsKey(key))
        //                    {
        //                        locationDictionary[key] = (int)reader["LocationId"];
        //                    }
        //                }
        //            }

        //            // Setup temporary table
        //            string tempTableName = "#tempWeatherData";
        //            command.CommandText = $@"
        //            CREATE TABLE {tempTableName} (
        //            TemperatureC DECIMAL(10, 2),
        //            WindSpeed DECIMAL(10, 2),
        //            WindDirection DECIMAL(10, 2),
        //            WindGust DECIMAL(10, 2),
        //            RelativeHumidity DECIMAL(10, 2),
        //            Rain DECIMAL(10, 2),
        //            GlobalTiltedIrRadiance DECIMAL(10, 2),
        //            DateAndTime DATETIME,
        //            LocationId INT,
        //            IsDeleted BIT
        //            );";

        //            command.ExecuteNonQuery();

        //            DataTable dataTable = new DataTable();
        //            dataTable.Columns.Add("TemperatureC", typeof(float));
        //            dataTable.Columns.Add("WindSpeed", typeof(float));
        //            dataTable.Columns.Add("WindDirection", typeof(float));
        //            dataTable.Columns.Add("WindGust", typeof(float));
        //            dataTable.Columns.Add("RelativeHumidity", typeof(float));
        //            dataTable.Columns.Add("Rain", typeof(float));
        //            dataTable.Columns.Add("GlobalTiltedIrRadiance", typeof(float));
        //            dataTable.Columns.Add("DateAndTime", typeof(DateTime));
        //            dataTable.Columns.Add("LocationId", typeof(int));
        //            dataTable.Columns.Add("IsDeleted", typeof(bool));

        //            foreach (var data in saveToStorageDtoDataList)
        //            {
        //                string locationKey = $"{data.Latitude},{data.Longitude}";
        //                if (locationDictionary.TryGetValue(locationKey, out int locationId))
        //                {
        //                    var weatherData = data.HistoricWeatherData;
        //                    for (int i = 0; i < weatherData.Hourly.Time.Length; i++)
        //                    {

        //                        DateTime parsedDate = DateTime.ParseExact(weatherData.Hourly.Time[i], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        //                        dataTable.Rows.Add(
        //                            Math.Round(weatherData.Hourly.Temperature_2m[i], 2),
        //                            Math.Round(weatherData.Hourly.Wind_Speed_10m[i], 2),
        //                            Math.Round(weatherData.Hourly.Wind_Direction_10m[i], 2),
        //                            Math.Round(weatherData.Hourly.Wind_Gusts_10m[i], 2),
        //                            Math.Round(weatherData.Hourly.Relative_Humidity_2m[i], 2),
        //                            Math.Round(weatherData.Hourly.Rain[i], 2),
        //                            Math.Round(weatherData.Hourly.Global_Tilted_Irradiance_Instant[i], 2),
        //                            parsedDate,
        //                            locationId,
        //                            false
        //                        );
        //                    }
        //                }
        //                else
        //                {
        //                    Debug.WriteLine("LocationId ikke fundet for nøglen: " + locationKey);
        //                }
        //            }

        //            if (dataTable.Rows.Count > 0)
        //            {
        //                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
        //                {
        //                    bulkCopy.DestinationTableName = tempTableName;
        //                    await bulkCopy.WriteToServerAsync(dataTable);

        //                    // MERGE operation
        //                    command.CommandText = $@"
        //                    MERGE INTO WeatherDatas AS target
        //                    USING {tempTableName} AS source
        //                    ON target.DateAndTime = source.DateAndTime AND target.LocationId = source.LocationId
        //                    WHEN NOT MATCHED THEN
        //                    INSERT (TemperatureC, WindSpeed, WindDirection, WindGust, RelativeHumidity, Rain, GlobalTiltedIrRadiance, DateAndTime, LocationId, IsDeleted)
        //                    VALUES (source.TemperatureC, source.WindSpeed, source.WindDirection, source.WindGust, source.RelativeHumidity, source.Rain, source.GlobalTiltedIrRadiance, source.DateAndTime, source.LocationId, source.IsDeleted);";
        //                    await command.ExecuteNonQueryAsync();
        //                }
        //            }
        //            else
        //            {
        //                Debug.WriteLine("Ingen data at indsætte.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Fejl under databaseoperation: " + ex.Message);
        //        throw;
        //    }
        //}





    }
}
