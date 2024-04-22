using DVF_API.Data.Interfaces;
using DVF_API.Data.Models;
using DVF_API.SharedLib.Dtos;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace DVF_API.Data.Repositories
{
    public class HistoricWeatherDataRepository : IHistoricWeatherDataRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public HistoricWeatherDataRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("WeatherDataDb");

        }




        public async Task SaveDataToFileAsync(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            await SaveDataAsBinaryAsync(data, latitude, longitude, baseFolder);
        }





        public async Task SaveDataToDatabaseAsync(HistoricWeatherDataDto data, string latitude, string longitude)
        {
            //Debug.WriteLine("Saving data to database...");

        }


        public async Task InsertCitiesToDB(List<City> cities)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO Cities (PostalCode, CityName)VALUES (@PostalCode, @CityName)";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.Add("@PostalCode", SqlDbType.VarChar, 255);
            command.Parameters.Add("@CityName", SqlDbType.VarChar, 255);

            foreach (City city in cities)
            {
                command.Parameters["@PostalCode"].Value = city.PostalCode;
                command.Parameters["@CityName"].Value = city.CityName;

                try
                {
                    int result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        Debug.WriteLine("No rows inserted. Check if the city with given PostalCode exists.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred: {ex.Message}");
                    // Ready for logging
                }
            }
        }



        public async Task InsertLocationsToDB(List<LocationDto> locations)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Forbered SQL-command en gang
            string query = @"INSERT INTO Locations (Latitude, Longitude, StreetName, StreetNumber, CityId) 
                     VALUES (@Latitude, @Longitude, @StreetName, @StreetNumber, 
                             (SELECT CityId FROM Cities WHERE PostalCode = @PostalCode))";

            await using SqlCommand command = new SqlCommand(query, connection);

            // Definer parametre én gang
            command.Parameters.Add("@Latitude", SqlDbType.VarChar, 255);
            command.Parameters.Add("@Longitude", SqlDbType.VarChar, 255);
            command.Parameters.Add("@StreetName", SqlDbType.VarChar, 255);
            command.Parameters.Add("@StreetNumber", SqlDbType.VarChar, 255);
            command.Parameters.Add("@PostalCode", SqlDbType.VarChar, 255);

            foreach (LocationDto location in locations)
            {
                command.Parameters["@Latitude"].Value = location.Latitude;
                command.Parameters["@Longitude"].Value = location.Longitude;
                command.Parameters["@StreetName"].Value = location.StreetName;
                command.Parameters["@StreetNumber"].Value = location.StreetNumber;
                command.Parameters["@PostalCode"].Value = location.PostalCode;

                try
                {
                    int result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        Debug.WriteLine("No rows inserted. Check if the city with given PostalCode exists.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred: {ex.Message}");
                    // Ready for logging
                }
            }
        }





        private async Task SaveDataAsBinaryAsync_old(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            var groupedData = data.Hourly.Time
                                    .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
                                    .GroupBy(t => t.Time.ToString("yyyyMMdd"))
                                    .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var entry in groupedData)
            {
                string dateKey = entry.Key;
                DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
                string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

                if (!Directory.Exists(yearFolder))
                    Directory.CreateDirectory(yearFolder);

                string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");  // End with .bin to indicate binary file

                using (var binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    foreach (var v in entry.Value)
                    {
                        // Convert each time point to float and write directly as binary
                        await Task.Run(() =>
                        {
                            binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[v.Index]));
                            binWriter.Write(data.Hourly.Temperature_2m[v.Index]);
                            binWriter.Write(data.Hourly.Relative_Humidity_2m[v.Index]);
                            binWriter.Write(data.Hourly.Rain[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Speed_10m[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Direction_10m[v.Index]);
                            binWriter.Write(data.Hourly.Wind_Gusts_10m[v.Index]);
                            binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]);
                        });
                    }
                }
            }
        }



        //private async Task SaveDataAsBinaryAsync(List<HistoricWeatherDataDto> allData, string baseFolder)
        //{
        //    await Task.Run(() =>
        //    {
        //        Parallel.ForEach(allData, async (data) =>
        //        {
        //            string latitude = data.Latitude;
        //            string longitude = data.Longitude;
        //            var yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", DateTime.Now.ToString("yyyy"));
        //            if (!Directory.Exists(yearFolder))
        //                Directory.CreateDirectory(yearFolder);

        //            string filePath = Path.Combine(yearFolder, $"{DateTime.Now:MMdd}.bin");
        //            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        //            using (var binWriter = new BinaryWriter(fileStream))
        //            {
        //                // Write your data here
        //                await fileStream.FlushAsync();
        //            }
        //        });
        //    });
        //}




        private async Task SaveDataAsBinaryAsync(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        {
            var timeStamps = data.Hourly.Time.Select(DateTime.Parse).ToList();
            var groupedData = timeStamps
                                .Select((time, index) => new { Time = time, Index = index })
                                .GroupBy(t => t.Time.ToString("yyyyMMdd"))
                                .ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToList());

            foreach (var entry in groupedData)
            {
                DateTime entryDate = DateTime.ParseExact(entry.Key, "yyyyMMdd", CultureInfo.InvariantCulture);
                string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

                if (!Directory.Exists(yearFolder))
                    Directory.CreateDirectory(yearFolder);

                string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                using (var binWriter = new BinaryWriter(fileStream))
                {
                    foreach (var index in entry.Value)
                    {
                        binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[index]));
                        binWriter.Write(data.Hourly.Temperature_2m[index]);
                        binWriter.Write(data.Hourly.Relative_Humidity_2m[index]);
                        binWriter.Write(data.Hourly.Rain[index]);
                        binWriter.Write(data.Hourly.Wind_Speed_10m[index]);
                        binWriter.Write(data.Hourly.Wind_Direction_10m[index]);
                        binWriter.Write(data.Hourly.Wind_Gusts_10m[index]);
                        binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[index]);
                    }
                    // Brug FileStream's FlushAsync metode i stedet for BinaryWriter
                    await fileStream.FlushAsync();
                }
            }
        }



        private float ConvertDateTimeToFloat(string time)
        {
            DateTime parsedDateTime = DateTime.Parse(time);
            return float.Parse(parsedDateTime.ToString("HHmm"));
        }



        //private void SaveDataAsBinary(HistoricWeatherDataDto data, string latitude, string longitude, string baseFolder)
        //{
        //    var groupedData = data.Hourly.Time
        //                        .Select((time, index) => new { Time = DateTime.Parse(time), Index = index })
        //                        .GroupBy(t => t.Time.ToString("yyyyMMdd"))
        //                        .ToDictionary(g => g.Key, g => g.ToList());

        //    foreach (var entry in groupedData)
        //    {
        //        string dateKey = entry.Key;
        //        DateTime entryDate = DateTime.ParseExact(dateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
        //        string yearFolder = Path.Combine(baseFolder, $"{latitude}-{longitude}", entryDate.ToString("yyyy"));

        //        if (!Directory.Exists(yearFolder))
        //            Directory.CreateDirectory(yearFolder);

        //        string filePath = Path.Combine(yearFolder, $"{entryDate:MMdd}.bin");  // End with .bin to indicate binary file

        //        using (var binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        //        {
        //            foreach (var v in entry.Value)
        //            {
        //                // Convert each time point to float and write directly as binary
        //                binWriter.Write(ConvertDateTimeToFloat(data.Hourly.Time[v.Index]));
        //                binWriter.Write(data.Hourly.Temperature_2m[v.Index]);
        //                binWriter.Write(data.Hourly.Relative_Humidity_2m[v.Index]);
        //                binWriter.Write(data.Hourly.Rain[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Speed_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Direction_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Wind_Gusts_10m[v.Index]);
        //                binWriter.Write(data.Hourly.Global_Tilted_Irradiance_Instant[v.Index]);
        //            }
        //        }
        //    }
        //}


    }
}
