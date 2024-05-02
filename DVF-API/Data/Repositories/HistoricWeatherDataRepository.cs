using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

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




        /// <summary>
        /// Calls the SaveDataAsBinaryFilesAsync method to save weather data to a binary file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="weatherStruct"></param>
        /// <returns></returns>
        public async Task SaveDataToFileAsync(string fileName, BinaryWeatherStructDto[] weatherStruct)
        {
             SaveDataAsBinaryFiles(fileName, weatherStruct);
        }




        /// <summary>
        /// Calls the InsertWeatherDataToDatabaseAsync method to insert weather data into the database.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="weatherStruct"></param>
        /// <returns>A Task.</returns>
        public async Task<bool> SaveDataToDatabaseAsync(DateTime date, BinaryWeatherStructDto[] weatherStruct)
        {
            return await InsertWeatherDataToDatabaseAsync(date, weatherStruct, _connectionString);
        }




        /// <summary>
        /// Calls the InsertCitiesToDB method to insert cities into the database.
        /// </summary>
        /// <param name="cities"></param>
        /// <returns>Returns a Task.</returns>
        public async Task SaveCitiesToDBAsync(List<City> cities)
        {
            await InsertCitiesToDB(cities);
        }




        /// <summary>
        /// Calls the InsertLocationsToDB method to insert locations into the database.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns>A Task.</returns>
        public async Task SaveLocationsToDBAsync(List<LocationDto> locations)
        {
            await InsertLocationsToDB(locations);
        }




        /// <summary>
        /// Calls the InsertCordinatesToDB method to insert coordinates into the database.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public async Task SaveCoordinatesToDBAsync(List<string> coordinates)
        {
            await InsertCordinatesToDB(coordinates);
        }




        /// <summary>
        /// Inserts cities into the database using a simple insert operation.
        /// </summary>
        /// <param name="cities"></param>
        /// <returns></returns>
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
                // Ready for logging
            }
        }




        /// <summary>
        /// Inserts locations into the database using a bulk copy operation.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns>Returns a Task.</returns>
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




        /// <summary>
        /// Inserts coordinates into the database using a bulk copy operation.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>Returns a Task.</returns>
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
                throw;  // Re throw to handle higher up or log with more details
            }
        }




        /// <summary>
        /// Saves the weather data to a binary file. using a byte array. The pointer is used to point where the data should be saved.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="weatherStruct"></param>
        private void SaveDataAsBinaryFiles(string fileName, BinaryWeatherStructDto[] weatherStruct)
        {
            // Calculate the total size needed for all structs
            long totalSize = (long)Marshal.SizeOf<BinaryWeatherStructDto>() * weatherStruct.Length;
            long structSize = Marshal.SizeOf<BinaryWeatherStructDto>();
            byte[] buffer = new byte[totalSize];

            unsafe
            {
                // Extract bytes from each struct and concatenate into one big byte array
                for (long i = 0; i < weatherStruct.Length; i++)
                {
                    // Calculate the start index in the buffer for the current struct
                    long bufferIndex = i * structSize;

                    // Copy source array to destination array
                    for (long j = 0; j < structSize; j++)
                    {
                        buffer[bufferIndex + j] = weatherStruct[i].BinaryWeatherDataByteArray[j];
                    }
                }
            }

            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: false))
                {
                     fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Ready for logging Access denied: {ex.Message}
            }
            catch (IOException ex)
            {
                // Ready for logging IO error: {ex.Message}
                if (ex is PathTooLongException)
                {
                    // Ready for logging The specified path, file name, or both exceed the system-defined maximum length.
                }
            }
            catch (Exception ex)
            {
                // Ready for logging A File writer error occurred: {ex.Message}
            }
        }




        /// <summary>
        /// Inserts weather data into the database, using a MERGE operation to speed up the process.
        /// </summary>
        /// <param name="saveToStorageDtoDataList"></param>
        /// <param name="connectionString"></param>
        /// <returns>Returns a Task.</returns>
        private async Task<bool> InsertWeatherDataToDatabaseAsync(DateTime date, BinaryWeatherStructDto[] weatherStructArray, string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand("", connection);
                    command.CommandTimeout = 600;
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
                LocationId BIGINT,
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
                    dataTable.Columns.Add("LocationId", typeof(long));
                    dataTable.Columns.Add("IsDeleted", typeof(bool));

                    unsafe
                    {
                        foreach (var weatherStruct in weatherStructArray)
                        {
                            float* data = weatherStruct.WeatherData; // Directly use the pointer

                            int hour = (int)data[0];
                            int minutes = (int)((data[0] - hour) * 100); // Extract minutes if they're represented in the decimal
                            DateTime recordDate = date.Date.AddHours(hour).AddMinutes(minutes);

                            dataTable.Rows.Add(
                                data[1],  // TemperatureC
                                data[2],  // WindSpeed
                                data[3],  // WindDirection
                                data[4],  // WindGust
                                data[5],  // RelativeHumidity
                                data[6],  // Rain
                                data[7],  // GlobalTiltedIrRadiance
                                recordDate,
                                weatherStruct.LocationId,
                                false
                            );
                        }
                        weatherStructArray = null; // Clear the array
                    }

                    if (dataTable.Rows.Count > 0)
                    {
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                        {
                            bulkCopy.DestinationTableName = tempTableName;
                            bulkCopy.BatchSize = 5000;
                            bulkCopy.BulkCopyTimeout = 600;
                            await bulkCopy.WriteToServerAsync(dataTable);
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
                        //Ready for logging
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //Ready for logging
                return false;
            }
        }
    }
}
