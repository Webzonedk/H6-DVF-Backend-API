using DVF_API.Data.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;

namespace DVF_API.Data.Repositories
{
    public class CrudFileRepository: ICrudFileRepository
    {

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/deletedWeatherData/";


        public CrudFileRepository()
        {
           
        }




        /// <summary>
        /// Loads weather data files based on the search criteria, adding the raw data to a list of byte arrays.
        /// </summary>
        /// <param name="search"></param>
        /// <returns>Returns a list of byte arrays containing the raw data.</returns>
        public async Task<List<byte[]>> FetchWeatherDataAsync(SearchDto search)
        {
            List<byte[]> rawDataFiles = new List<byte[]>();
            foreach (string coordinate in search.Coordinates)
            {
                string path = Path.Combine(_baseDirectory, coordinate);
                foreach (int year in Enumerable.Range(search.FromDate.Year, search.ToDate.Year - search.FromDate.Year + 1))
                {
                    string yearPath = Path.Combine(path, year.ToString());
                    if (Directory.Exists(yearPath))
                    {
                        var files = Directory.GetFiles(yearPath, "*.bin", SearchOption.TopDirectoryOnly);
                        foreach (string file in files)
                        {
                            if (IsFileDateWithinRange(file, search.FromDate, search.ToDate))
                            {
                                byte[] rawData = await File.ReadAllBytesAsync(file);
                                rawDataFiles.Add(rawData);
                            }
                        }
                    }
                }
            }
            return rawDataFiles;
        }





        public void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            throw new NotImplementedException();
        }




        public void RestoreAllData()
        {
            throw new NotImplementedException();
        }




        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// Checks if the file date is within the given date range.
        /// </summary>
        /// <param name="filePath">File path to check.</param>
        /// <param name="fromDate">Start date of the range.</param>
        /// <param name="toDate">End date of the range.</param>
        /// <returns>True if within range, otherwise false.</returns>
        private bool IsFileDateWithinRange(string filePath, DateTime fromDate, DateTime toDate)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            DateTime fileDate = DateTime.ParseExact(fileName, "MMdd", CultureInfo.InvariantCulture);
            return fileDate >= fromDate && fileDate <= toDate;
        }






        ///// <summary>
        ///// Loads weather data files based on the search criteria.
        ///// </summary>
        ///// <param name="search">The search criteria.</param>
        ///// <returns>A list of WeatherDataFileDto objects.</returns>
        //public List<WeatherDataFileDto> LoadWeatherData(SearchDto search)
        //{
        //    List<WeatherDataFileDto> dataFiles = new List<WeatherDataFileDto>();
        //    foreach (string coordinate in search.Coordinates)
        //    {
        //        string path = Path.Combine(_baseDirectory, coordinate);
        //        foreach (int year in Enumerable.Range(search.FromDate.Year, search.ToDate.Year - search.FromDate.Year + 1))
        //        {
        //            string yearPath = Path.Combine(path, year.ToString());
        //            if (Directory.Exists(yearPath))
        //            {
        //                foreach (string file in Directory.GetFiles(yearPath, "*.bin", SearchOption.TopDirectoryOnly))
        //                {
        //                    if (IsFileDateWithinRange(file, search.FromDate, search.ToDate))
        //                    {
        //                        WeatherDataFileDto weatherData = ReadWeatherDataFile(file);
        //                        if (weatherData != null)
        //                        {
        //                            dataFiles.Add(weatherData);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return dataFiles;
        //}






        ///// <summary>
        ///// Checks if the file date is within the given date range.
        ///// </summary>
        ///// <param name="filePath">File path to check.</param>
        ///// <param name="fromDate">Start date of the range.</param>
        ///// <param name="toDate">End date of the range.</param>
        ///// <returns>True if within range, otherwise false.</returns>
        //private bool IsFileDateWithinRange(string filePath, DateTime fromDate, DateTime toDate)
        //{
        //    string fileName = Path.GetFileNameWithoutExtension(filePath);
        //    DateTime fileDate = DateTime.ParseExact(fileName, "MMdd", CultureInfo.InvariantCulture);
        //    return fileDate >= fromDate && fileDate <= toDate;
        //}




        //private WeatherDataFileDto ReadWeatherDataFile(string filePath)
        //{
        //    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        //    using (BinaryReader reader = new BinaryReader(fs))
        //    {
        //        int numEntries = (int)fs.Length / 32; // Each entry has 8 float values, each float being 4 bytes.
        //        WeatherDataFileDto data = new WeatherDataFileDto
        //        {
        //            Time = new float[numEntries],
        //            Temperature_2m = new float[numEntries],
        //            Relative_Humidity_2m = new float[numEntries],
        //            Rain = new float[numEntries],
        //            Wind_Speed_10m = new float[numEntries],
        //            Wind_Direction_10m = new float[numEntries],
        //            Wind_Gusts_10m = new float[numEntries],
        //            Global_Tilted_Irradiance_Instant = new float[numEntries]
        //        };

        //        for (int i = 0; i < numEntries; i++)
        //        {
        //            data.Time[i] = reader.ReadSingle();
        //            data.Temperature_2m[i] = reader.ReadSingle();
        //            data.Relative_Humidity_2m[i] = reader.ReadSingle();
        //            data.Rain[i] = reader.ReadSingle();
        //            data.Wind_Speed_10m[i] = reader.ReadSingle();
        //            data.Wind_Direction_10m[i] = reader.ReadSingle();
        //            data.Wind_Gusts_10m[i] = reader.ReadSingle();
        //            data.Global_Tilted_Irradiance_Instant[i] = reader.ReadSingle();
        //        }

        //        return data;
        //    }
        //}



    }
}
