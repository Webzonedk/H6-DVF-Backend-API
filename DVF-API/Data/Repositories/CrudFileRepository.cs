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
        public async Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(Dictionary<string, List<BinarySearchInFilesDto>> binarySearchInFilesDtos)
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




        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            throw new NotImplementedException();
        }


        private async Task<List<BinaryDataFromFileDto>> ReadWeatherDataAsync(Dictionary<string, List<BinarySearchInFilesDto>> binarySearchInFilesDtos)
        {





            List<BinaryDataFromFileDto> binaryDataFromFileDtos = new List<BinaryDataFromFileDto>();
            long keyCount = binarySearchInFilesDtos.Keys.Count;
            long listLength = binarySearchInFilesDtos.Values.First().Count;
            // long bufferSize = keyCount * listLength * 960;
            long singleBufferSize = listLength * 960;
            //  byte[] data = new byte[bufferSize];
            //  ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;
            // Dictionary<string, byte[]> BytesFromFileToOutput = new Dictionary<string, byte[]>();
            ConcurrentDictionary<string, byte[]> BytesFromFileToOutputDictionary = new ConcurrentDictionary<string, byte[]>();
            foreach (var file in binarySearchInFilesDtos)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        if (File.Exists(file.Key))
                        {
                            Debug.WriteLine($"found filepath: {file.Key}---------------------");

                            using (FileStream stream = new FileStream(file.Key!, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                            {
                                foreach (var byteRange in file.Value)
                                {
                                    int bytesRead = (int)(byteRange.ToByte - byteRange.FromByte);
                                    byte[] buffer = new byte[bytesRead];

                                    try
                                    {
                                        stream.Seek(byteRange.FromByte, SeekOrigin.Begin);
                                        int bytesReadTotal = await stream.ReadAsync(buffer, 0, bytesRead);
                                        if (bytesReadTotal != bytesRead)
                                        {
                                            throw new IOException("Not all bytes were read.");
                                        }

                                        BinaryDataFromFileDto binaryDataFromFileDto = new BinaryDataFromFileDto()
                                        {
                                            BinaryWeatherData = buffer.AsSpan(0, bytesRead).ToArray(), // Create a copy of the data read
                                            YearDate = file.Key
                                        };

                                        lock (lockObject)
                                        {
                                            binaryDataFromFileDtos.Add(binaryDataFromFileDto);


                                            //BytesFromFileToOutputDictionary.AddOrUpdate(file.Key, k => buffer, (k, existingValue) =>
                                            //{
                                            //    byte[] combinedValue = new byte[existingValue.Length + buffer.Length];
                                            //    Buffer.BlockCopy(existingValue, 0, combinedValue, 0, existingValue.Length);
                                            //    Buffer.BlockCopy(buffer, 0, combinedValue, existingValue.Length, buffer.Length);
                                            //    return combinedValue;
                                            //});




                                            //if (BytesFromFileToOutputDictionary.ContainsKey(file.Key))
                                            //{
                                            //    byte[] currentByteArray = BytesFromFileToOutputDictionary[file.Key];
                                            //    currentByteArray = currentByteArray.Concat(buffer).ToArray();
                                            //    BytesFromFileToOutputDictionary[file.Key] = currentByteArray;
                                            //}
                                            //else
                                            //{
                                            //}
                                        }
                                    }
                                    finally
                                    {
                                        // bufferPool.Return(buffer); // Return buffer to the pool
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

        //    await Task.WhenAll(tasks);
        //    tasks.Clear();
        //    binarySearchInFilesDtos.Clear();
        //    return binaryDataFromFileDtos;


        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        //private async Task<List<BinaryDataFromFileDto>> ReadWeatherDataAsync(Dictionary<string, List<BinarySearchInFilesDto>> binarySearchInFilesDtos)
        //{

        //    List<Task> tasks = new List<Task>();
        //    object lockObject = new object();
        //    SemaphoreSlim semaphore = new SemaphoreSlim(_utilityManager.CalculateOptimalDegreeOfParallelism());




        //    List<BinaryDataFromFileDto> binaryDataFromFileDtos = new List<BinaryDataFromFileDto>();
        //    foreach (var file in binarySearchInFilesDtos)
        //    {

        //        tasks.Add(Task.Run(async () =>
        //        {
        //            await semaphore.WaitAsync();

        //            try
        //            {
        //                //skip if filepath does not exists
        //                if (File.Exists(file.Key))
        //                {
        //                    Debug.WriteLine($"found filepath: {file.Key}---------------------");
        //                    using (FileStream stream = new FileStream(file.Key!, FileMode.Open, FileAccess.Read,FileShare.Read,4096,true))
        //                    {
        //                        foreach (var byteRange in file.Value)
        //                        {

        //                            // Seek to the start byte position
        //                            stream.Seek(byteRange.FromByte, SeekOrigin.Begin);

        //                            // Calculate the number of bytes to read
        //                            int bytesRead = (int)(byteRange.ToByte - byteRange.FromByte);

        //                            // Read the bytes into a buffer
        //                            byte[] buffer = new byte[bytesRead];
        //                            int bytesReadTotal = await stream.ReadAsync(buffer, 0, bytesRead);
        //                            if (bytesReadTotal != bytesRead)
        //                            {
        //                                // Not all bytes were read
        //                                throw new IOException("Not all bytes were read.");
        //                            }

        //                            BinaryDataFromFileDto binaryDataFromFileDto = new BinaryDataFromFileDto()
        //                            {
        //                                BinaryWeatherData = buffer,
        //                                YearDate = file.Key

        //                            };

        //                            lock (lockObject)
        //                            {
        //                                binaryDataFromFileDtos.Add(binaryDataFromFileDto);
        //                            }

        //                        }
        //                    }
        //                }

        //            }
        //            catch (Exception e)
        //            {
        //                Debug.WriteLine(e);

        //            }

        //            finally
        //            {
        //                semaphore.Release();
        //            }


        //        }));

        //    }

        //    await Task.WhenAll(tasks);
        //    tasks.Clear();
        //    binarySearchInFilesDtos.Clear();
        //    return binaryDataFromFileDtos;


        //}




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
