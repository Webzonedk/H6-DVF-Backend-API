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
        public async Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(SearchDto search)
        {
            List<BinaryDataFromFileDto> binaryDataFromFileDtos = new List<BinaryDataFromFileDto>();

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
                                string yearDateString = string.Concat(year, Path.GetFileNameWithoutExtension(file));
                                byte[] rawData = await File.ReadAllBytesAsync(file);
                                BinaryDataFromFileDto binaryDataFromFileDto = new BinaryDataFromFileDto
                                {
                                    Coordinates = coordinate,
                                    YearDate = yearDateString,
                                    BinaryWeatherData = rawData
                                };
                                binaryDataFromFileDtos.Add(binaryDataFromFileDto);
                               
                            }
                        }
                    }
                }
            }
            return binaryDataFromFileDtos;
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
    }
}
