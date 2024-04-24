using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;
using System.Threading.Tasks.Dataflow;

namespace DVF_API.Data.Repositories
{
    public class CrudFileRepository : ICrudFileRepository
    {

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/deletedWeatherData/";
        private readonly IUtilityManager _utilityManager;

        public CrudFileRepository(IUtilityManager utilityManager)
        {
            _utilityManager = utilityManager;
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




        public async Task DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            await MoveOldFilesToDeletedDirectoryAsync(deleteWeatherDataBeforeThisDate);
        }




        public async Task RestoreAllData()
        {
            await RestoreFilesInDirectoryAsync(_baseDirectory, _deletedFilesDirectory);
        }




        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// Moves all files older than the given date to the deleted files directory, asynchronously.
        /// </summary>
        /// <param name="olderThanDate"></param>
        /// <returns>Returns a task.</returns>
        private async Task MoveOldFilesToDeletedDirectoryAsync(DateTime olderThanDate)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = _utilityManager.CalculateOptimalDegreeOfParallelism() };

            // Asynkron metode for at flytte filer rekursivt
            void MoveFilesInDirectoryAsync(string currentDirectory)
            {
                var directories = Directory.GetDirectories(currentDirectory);
                Parallel.ForEach(directories, options, async (directory) =>
                {
                    MoveFilesInDirectoryAsync(directory);
                });

                var files = Directory.GetFiles(currentDirectory, "*", SearchOption.TopDirectoryOnly);
                Parallel.ForEach(files, options, async (file) =>
                {
                    DateTime fileCreationDate = File.GetCreationTime(file);
                    if (fileCreationDate < olderThanDate)
                    {
                        string relativePath = Path.GetRelativePath(_baseDirectory, file);
                        string targetPath = Path.Combine(_deletedFilesDirectory, relativePath);
                        string targetDirectory = Path.GetDirectoryName(targetPath);

                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }

                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }

                        File.Move(file, targetPath);
                    }
                });
            }
            await Task.Run(() => MoveFilesInDirectoryAsync(_baseDirectory));
        }




        /// <summary>
        /// Restores all files from the deleted files directory to the base directory, asynchronously by moving them back to the base directory.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="deletedFilesDirectory"></param>
        /// <returns>Returns a task.</returns>
        private async Task RestoreFilesInDirectoryAsync(string baseDirectory, string deletedFilesDirectory)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = _utilityManager.CalculateOptimalDegreeOfParallelism() };

            async Task RestoreFilesAsync(string currentDeletedDirectory, string currentBaseDirectory)
            {
                var directories = Directory.GetDirectories(currentDeletedDirectory);
                Parallel.ForEach(directories, options, async (directory) =>
                {
                    string subFolder = Path.GetFileName(directory);
                    string newBaseDir = Path.Combine(currentBaseDirectory, subFolder);
                    if (!Directory.Exists(newBaseDir))
                    {
                        Directory.CreateDirectory(newBaseDir);
                    }
                    await RestoreFilesAsync(directory, newBaseDir);
                });

                var files = Directory.GetFiles(currentDeletedDirectory, "*", SearchOption.TopDirectoryOnly);
                Parallel.ForEach(files, options, async (file) =>
                {
                    string fileName = Path.GetFileName(file);
                    string targetPath = Path.Combine(currentBaseDirectory, fileName);

                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath); 
                    }

                    File.Move(file, targetPath); 
                });
            }

            await Task.Run(() => RestoreFilesAsync(deletedFilesDirectory, baseDirectory));
        }




        /// <summary>
        /// Checks if the file date is within the given date range. It is used within the FetchWeatherDataAsync method.
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
