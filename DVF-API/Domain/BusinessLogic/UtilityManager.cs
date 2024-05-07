using DVF_API.Domain.Interfaces;
using FluentAssertions.Common;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;

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

        #region Fields
        private const string _verySecretPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private static Dictionary<string, (DateTime lastAttempt, int attemptCount)> _loginAttempts = new();
        #endregion




        /// <summary>
        /// A simple password-based authentication method that checks if the provided password matches the predefined secret password.
        /// If the password is incorrect, the method will increment the login attempt count and block access after 5 failed attempts within 5 minutes.
        /// to avoid brute force attacks.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public bool Authenticate(string password, string clientIp)
        {
            if (_loginAttempts.TryGetValue(clientIp, out var loginInfo))
            {
                if (DateTime.UtcNow - loginInfo.lastAttempt < TimeSpan.FromMinutes(5) && loginInfo.attemptCount >= 5)
                {
                    throw new Exception("Too many failed attempts. Please try again later.");
                }
            }

            if (password != _verySecretPassword)
            {
                var attemptCount = loginInfo.attemptCount + 1;
                _loginAttempts[clientIp] = (DateTime.UtcNow, attemptCount);
                throw new UnauthorizedAccessException("Unauthorized - Incorrect password");
            }

            // Reset the login attempts on successful password verification
            _loginAttempts.Remove(clientIp);
            return true;
        }




        /// <summary>
        /// Calculates the optimal degree of parallelism based on the available system resources, to avoid overloading the system.
        /// this method has no Unit Test because it is just measuring the available memory and processor count
        /// </summary>
        /// <returns>An integer value representing the optimal degree of parallelism based on the system configuration.</returns>
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




        /// <summary>
        /// Get the available memory in bytes based on the operating system.
        /// This method has no Unit Test because it is a private method that is called by other public methods, and the result is dependent on the operating system.
        /// </summary>
        /// <returns>A long value representing the available memory in bytes.</returns>
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




        /// <summary>
        /// returns number of bytes this object contains
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetModelSize(object obj)
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(obj);
            return jsonBytes.Length;
        }




        /// <summary>
        /// Converts the given number of bytes to a human-readable format (e.g., KB, MB, GB).
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>a string representing the number of bytes in a human-readable format.</returns>
        public string ConvertBytesToFormat(double bytes)
        {
            double KB = 1024;
            double MB = KB * 1024;
            double GB = MB * 1024;
            if (bytes < KB)
            {
                return $"{Math.Round(bytes, 2, MidpointRounding.AwayFromZero)} bytes";
            }
            else if (bytes < MB)
            {
                return $"{Math.Round(bytes / KB, 2, MidpointRounding.AwayFromZero)} KB";
            }
            else if (bytes < GB)
            {
                return $"{Math.Round(bytes / MB, 2, MidpointRounding.AwayFromZero)} MB";
            }
            else
            {
                return $"{Math.Round(bytes / GB, 2, MidpointRounding.AwayFromZero)} GB";
            }
        }




        /// <summary>
        ///  Converts the given time measurement to a human-readable format (e.g., milliseconds, seconds, minutes).
        /// </summary>
        /// <param name="time"></param>
        /// <returns>a string representing the time measurement in a human-readable format.</returns>
        public string ConvertTimeMeasurementToFormat(double time)
        {

            if (time < 1000)
            {
                return $"{time.ToString("0.##")} ms";
            }
            else if (time < 60_000)
            {
                return $"{(time / 1000).ToString("0.##")} sec";
            }
            else
            {
                return $"{(time / 60_000).ToString("0.##")} min";
            }
        }




        /// <summary>
        /// Converts a given date and time to a long value in
        /// </summary>
        /// <param name="time"></param>
        /// <returns>a double value representing the date and time in float format. Alternatively, returns 0 on error.</returns>
        public long ConvertDateTimeToDouble(string time)
        {
            DateTime parsedDateTime = DateTime.Parse(time);
            return long.Parse(parsedDateTime.ToString("yyyyMMddHHmm"));
        }




        /// <summary>
        /// Splits a double representation of a date and time into separate components.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>an object array containing the date and time components.</returns>
        public object[] MixedYearDateTimeSplitter(double time)
        {
            string timeString;
            object[] result = new object[2];

            try
            {
                if (time < 0 || time > 999999999999)
                {
                    throw new ArgumentOutOfRangeException(nameof(time), "Input is out of range for a valid date-time representation.");
                }

                timeString = time.ToString("000000000000");
                result[0] = timeString.Substring(0, 8);
                result[1] = float.Parse(timeString.Substring(8, 4));
            }
            catch
            {
                result[0] = "00000000";
                result[1] = 0f;
            }
            return result;
        }




        /// <summary>
        /// Method to start measuring CPU time
        /// </summary>
        /// <returns>A tuple containing the initial CPU time and a Stopwatch object.</returns>
        public (TimeSpan, Stopwatch) BeginMeasureCPUTime()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            TimeSpan startTime = Process.GetCurrentProcess().TotalProcessorTime;
            return (startTime, stopwatch);
        }




        /// <summary>
        /// Method to stop measuring CPU time and calculate the CPU usage percentage.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="stopwatch"></param>
        /// <returns>A tuple containing the CPU usage percentage and the elapsed time in milliseconds.</returns>
        public (double CpuUsagePercentage, double ElapsedTimeMs) StopMeasureCPUTime(TimeSpan startTime, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            TimeSpan endTime = Process.GetCurrentProcess().TotalProcessorTime;
            TimeSpan cpuUsed = endTime - startTime;
            double cpuUsagePercentage = (cpuUsed.TotalMilliseconds / stopwatch.ElapsedMilliseconds) * 100;
            return (cpuUsagePercentage, stopwatch.ElapsedMilliseconds);
        }




        /// <summary>
        /// Method to start measuring memory
        /// </summary>
        /// <returns>A double value representing the initial memory usage in bytes.</returns>
        public double BeginMeasureMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }




        /// <summary>
        /// Method to stop measuring memory and calculate the memory usage.
        /// </summary>
        /// <param name="startMemory"></param>
        /// <returns>A double value representing the memory usage in bytes.</returns>
        public double StopMeasureMemory(double startMemory)
        {
            double endMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            return endMemory - startMemory;
        }
    }
}
