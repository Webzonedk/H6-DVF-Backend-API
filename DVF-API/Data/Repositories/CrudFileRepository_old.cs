using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks.Dataflow;

namespace DVF_API.Data.Repositories
{
    public class CrudFileRepository_old : ICrudFileRepository
    {

        private readonly IUtilityManager _utilityManager;

        public CrudFileRepository_old(IUtilityManager utilityManager)
        {
            _utilityManager = utilityManager;
        }




        /// <summary>
        /// Loads weather data files based on the search criteria, adding the raw data to a list of byte arrays.
        /// </summary>
        /// <param name="search"></param>
        /// <returns>Returns a list of byte arrays containing the raw data.</returns>
        public async Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(string baseDirectory, SearchDto search)
        { 
            return await ReadWeatherDataAsync(baseDirectory, search);
        }




        /// <summary>
        /// Calls the method to delete old data from the file system.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="deletedFilesDirectory"></param>
        /// <param name="deleteWeatherDataBeforeThisDate"></param>
        /// <returns>Returns a task.</returns>
        public async Task DeleteOldData(string baseDirectory, string deletedFilesDirectory, DateTime deleteWeatherDataBeforeThisDate)
        {
            await MoveOldFilesToDeletedDirectoryAsync(baseDirectory, deletedFilesDirectory, deleteWeatherDataBeforeThisDate);
        }




        /// <summary>
        /// Calls the method to restore all data from the deleted files directory to the base directory.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="deletedFilesDirectory"></param>
        /// <returns>Returns a task.</returns>
        public async Task RestoreAllData(string baseDirectory, string deletedFilesDirectory)
        {
            await RestoreFilesInDirectoryAsync(baseDirectory, deletedFilesDirectory);
        }




        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        private async Task<List<BinaryDataFromFileDto>> ReadWeatherDataAsync(string baseDirectory, SearchDto search)
        {
            List<BinaryDataFromFileDto> binaryDataFromFileDtos = new List<BinaryDataFromFileDto>();

            foreach (string coordinate in search.Coordinates)
            {
                string path = Path.Combine(baseDirectory, coordinate);
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




        /// <summary>
        /// Checks if the file date is within the given date range. It is used within the FetchWeatherDataAsync method.
        /// </summary>
        /// <param name="filePath">File path to check.</param>
        /// <param name="fromDate">Start date of the range.</param>
        /// <param name="toDate">End date of the range.</param>
        /// <returns>True if within range, otherwise false.</returns>
        private bool IsFileDateWithinRange(string filePath, DateOnly fromDate, DateOnly toDate)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            DateOnly fileDate = DateOnly.ParseExact(fileName, "MMdd", CultureInfo.InvariantCulture);
            return fileDate >= fromDate && fileDate <= toDate;
        }




        /// <summary>
        /// Asynchronously moves old files to a specified 'deleted' directory if they are older than the provided date.
        /// </summary>
        /// <param name="baseDirectory">Base directory to search for old files.</param>
        /// <param name="deletedFilesDirectory">Target directory where old files will be moved.</param>
        /// <param name="olderThanDate">Date threshold; files created before this date will be moved.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        private async Task MoveOldFilesToDeletedDirectoryAsync(string baseDirectory, string deletedFilesDirectory, DateTime olderThanDate)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = _utilityManager.CalculateOptimalDegreeOfParallelism() };

            // Asynchronous method to recursively move files
            void MoveFilesInDirectoryAsync(string currentDirectory)
            {
                try
                {
                    var directories = Directory.GetDirectories(currentDirectory);
                    Parallel.ForEach(directories, options, async (directory) =>
                    {
                        MoveFilesInDirectoryAsync(directory);
                    });

                    var files = Directory.GetFiles(currentDirectory, "*", SearchOption.TopDirectoryOnly);
                    Parallel.ForEach(files, options, async (file) =>
                    {
                        try
                        {
                            DateTime fileCreationDate = File.GetCreationTime(file);
                            if (fileCreationDate < olderThanDate)
                            {
                                string relativePath = Path.GetRelativePath(baseDirectory, file);
                                string targetPath = Path.Combine(deletedFilesDirectory, relativePath);
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
                        }
                        catch (Exception ex)
                        {
                            // Log the error with the problematic file and the exception details
                            Debug.WriteLine($"Error moving file {file}: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Log the error for issues within the current directory processing
                    Debug.WriteLine($"Error processing directory {currentDirectory}: {ex.Message}");
                }
            }

            try
            {
                await Task.Run(() => MoveFilesInDirectoryAsync(baseDirectory));
            }
            catch (Exception ex)
            {
                // Log the error for issues initiating the process
                Debug.WriteLine($"Error initiating process for base directory {baseDirectory}: {ex.Message}");
            }
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








    }
}
