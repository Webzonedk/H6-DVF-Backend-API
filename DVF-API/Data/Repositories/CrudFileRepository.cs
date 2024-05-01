using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace DVF_API.Data.Repositories
{
    public class CrudFileRepository : ICrudFileRepository
    {

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
        public async Task<BinaryWeatherStructDto[]> FetchWeatherDataAsync(BinarySearchInFilesDto binarySearchInFilesDtos)
        {
            return await ReadWeatherDataAsync(binarySearchInFilesDtos);
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



        /// <summary>
        /// calls the method to retrieve weatherdata base on all locations within a period or a single location
        /// </summary>
        /// <param name="weatherDataFromIOT"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            throw new NotImplementedException();
        }


        private unsafe async Task<BinaryWeatherStructDto[]> ReadWeatherDataAsync(BinarySearchInFilesDto binarySearchInFilesDtos)
        {
            // Calculate the total size needed for all structs
            int structSize = Marshal.SizeOf<BinaryWeatherStructDto>();
            FileInfo fileInfo = new FileInfo(binarySearchInFilesDtos.FilePath);
            // int totalSize = (int)fileInfo.Length;
            long numStructs = (binarySearchInFilesDtos.ToByte - binarySearchInFilesDtos.FromByte) / structSize + 1;
            string? filepath = binarySearchInFilesDtos.FilePath;


            BinaryWeatherStructDto[] binaryWeatherStructDtos = new BinaryWeatherStructDto[numStructs];

            try
            {
                if (File.Exists(filepath))
                {
                    Debug.WriteLine($"found filepath: {filepath}---------------------");

                    using (FileStream stream = new FileStream(filepath!, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {

                        int bufferSize = (int)(binarySearchInFilesDtos.ToByte - binarySearchInFilesDtos.FromByte);
                        long offset = binarySearchInFilesDtos.FromByte;
                        byte[] buffer = new byte[bufferSize];
                        stream.Seek(offset, SeekOrigin.Begin);
                        int bytesRead = stream.Read(buffer, 0, bufferSize);



                        fixed (byte* pBuffer = buffer)
                        {
                            for (int i = 0; i < numStructs; i++)
                            {
                                binaryWeatherStructDtos[i] = *(BinaryWeatherStructDto*)(pBuffer + i * structSize);
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error occured:--------------{e}");
            }

            return binaryWeatherStructDtos;


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
                            string[] parts = file.Split(new[] { '/', '\\', '.' }, StringSplitOptions.RemoveEmptyEntries).Where(part => part.Any(char.IsDigit)).ToArray();
                            string dateString = string.Join("", parts);
                            DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate);

                            if (fileDate < olderThanDate)
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
