using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.Services.ServiceImplementation;
using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text;

namespace DVF_API.Domain.BusinessLogic
{
    /// <summary>
    /// Provides methods for performing system maintenance tasks such as freeing up system resources,
    /// clearing the console, and terminating redundant processes associated with the current application.
    /// This class is designed to ensure that the application environment is clean and that resources
    /// are managed efficiently, especially before shutdown or when resource reallocation is necessary.
    /// </summary>
    public class UtilityManager : IUtilityManager
    {


        public UtilityManager()
        {
        }




        /// <summary>
        /// Cleans up system resources by forcing garbage collection, clearing the console window,
        /// and terminating all instances of the current process except the main one.
        /// This method is intended to be used cautiously, primarily in scenarios where explicit
        /// control over resource management is required, such as in preparation for application shutdown
        /// or after a significant change in application state that involves a large number of temporary objects.
        /// </summary>
        public void CleanUpRessources()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    process.Kill();
                }
            }
        }




        public int CalculateOptimalDegreeOfParallelism()
        {
            var maxCoreCount = Environment.ProcessorCount - 2;
            long availableMemoryBytes = GetAvailableMemory();
            float availableMemoryMb = availableMemoryBytes / (1024f * 1024f);  // convert to MB


            if (availableMemoryMb < 1024)
            {
                return Math.Max(1, maxCoreCount / 2);
            }
            else if (availableMemoryMb < 2048)
            {
                return Math.Max(1, (int)(maxCoreCount * 0.65));
            }
            return maxCoreCount;
        }




        private long GetAvailableMemory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var freeMemory = (ulong)obj["FreePhysicalMemory"];
                        return (long)freeMemory * 1024;
                    }
                }
                return 0;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string memInfoPath = "/proc/meminfo";
                long availableMemory = 0;
                string line;
                using (StreamReader reader = new StreamReader(memInfoPath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("MemAvailable:"))
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            availableMemory = long.Parse(parts[1]); // KB
                            break;
                        }
                    }
                }
                return availableMemory * 1024; // Returns bytes
            }
            return 0;
        }


        public int GetModelSize(object obj)
        {
            // Calculate memory size using Marshal.SizeOf
            //return Marshal.SizeOf(obj);

            string jsonString = JsonSerializer.Serialize(obj);
            return Encoding.UTF8.GetBytes(jsonString).Length;
        }

        public int GetModelSize<T>(List<T> list)
        {
            // Calculate memory size of the list
            int totalSize = 0;
            foreach (var item in list)
            {
                totalSize += GetModelSize(item);
            }
            return totalSize;
        }

        /// <summary>
        /// converts number of bytes to MB
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public float ConvertBytesToMegabytes(int bytes)
        {
            return (float)bytes / (1024 * 1024);
        }

        /// <summary>
        /// converts number of bytes to GB
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public float ConvertBytesToGigabytes(int bytes)
        {
            return (float)bytes / (1024 * 1024 * 1024);
        }

       

        /// <summary>
        /// begins measuring the time and process of current process, returns the elapsed time and a stopwatch object
        /// </summary>
        /// <returns></returns>
        public (TimeSpan, Stopwatch) BeginMeasureCPU()
        {
            // Get CPU usage before executing the code
            Process processBefore = Process.GetCurrentProcess();
            TimeSpan cpuTimeBefore = processBefore.TotalProcessorTime;

            // Begin monitoring time spent, cpu usage, and ram
            Stopwatch stopwatch = Stopwatch.StartNew();

            return (cpuTimeBefore, stopwatch);
        }

        public (float CpuUsage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan cpuTimeBefore, Stopwatch stopwatch)
        {
            // Record end time
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;

            // Get CPU usage after executing the code
            Process processAfter = Process.GetCurrentProcess();
            TimeSpan cpuTimeAfter = processAfter.TotalProcessorTime;

            // Calculate CPU usage during the execution of the code
            TimeSpan cpuTimeUsed = cpuTimeAfter - cpuTimeBefore;
            float cpuUsage = (float)((cpuTimeUsed.TotalMilliseconds / elapsedTime.TotalMilliseconds) * 100);

            return (CpuUsage: cpuUsage, ElapsedTimeMs: (float)elapsedTime.TotalMilliseconds);
        }

        /// <summary>
        /// records amount of bytes for current process running
        /// </summary>
        /// <returns></returns>
        public (Process currentProcess, long processBytes) BeginMeasureMemory()
        {
            // Get the current process
            Process process = Process.GetCurrentProcess();
            long ramUsageBeforeBytes = process.WorkingSet64;
            // Get the RAM usage before executing the code block
            return (currentProcess: process, processBytes: ramUsageBeforeBytes);
        }

        /// <summary>
        /// stops measuring ram usage, takes in a process and an 64 bit int, returns 64 bit int as well
        /// </summary>
        /// <param name="ramUsageBeforeBytes"></param>
        /// <param name="currentProcess"></param>
        /// <returns></returns>
        public long StopMeasureMemory(long ramUsageBeforeBytes, Process currentProcess)
        {
            // Get the RAM usage after executing the code block
            long ramUsageAfterBytes = currentProcess.WorkingSet64;

            // Calculate the difference in RAM usage
            return ramUsageAfterBytes - ramUsageBeforeBytes;
        }
    }
}
