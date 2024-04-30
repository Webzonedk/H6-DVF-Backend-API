using DVF_API.Domain.Interfaces;
using System.Diagnostics;
using System.Globalization;
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
        private const string VerySecretPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private static Dictionary<string, (DateTime lastAttempt, int attemptCount)> _loginAttempts = new();


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

            if (password != VerySecretPassword)
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
        public string ConvertBytesToFormat(long bytes)
        {
            int KB = 1024;
            int MB = KB * 1024;
            int GB = MB * 1024;

            if (bytes < KB)
            {
                return $"{bytes} bytes";
            }
            else if (bytes < MB)
            {
                return $"{bytes / KB} KB";
            }
            else if (bytes < GB)
            {
                return $"{bytes / MB} MB";
            }
            else
            {
                return $"{bytes / GB} GB";
            }
        }




        /// <summary>
        ///  Converts the given time measurement to a human-readable format (e.g., milliseconds, seconds, minutes).
        /// </summary>
        /// <param name="time"></param>
        /// <returns>a string representing the time measurement in a human-readable format.</returns>
        public string ConvertTimeMeasurementToFormat(float time)
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

        //public double ConvertDateTimeToDouble(string date)
        //{
        //    try
        //    {
        //        DateTime parsedDateTime;
        //        string[] formats = { "dd-MM-yyyy", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd" };

        //        if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
        //        {
        //            string formattedDateTime = parsedDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        //            Debug.WriteLine($"Formatted DateTime: {formattedDateTime}");
        //            double result = double.Parse(formattedDateTime, CultureInfo.InvariantCulture);
        //            Debug.WriteLine($"Parsed Double: {result}");
        //            return result;
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Unable to parse date.");
        //            return -1; // Eller en anden fejlkode
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error parsing date or converting to double: {ex.Message}. Stack Trace: {ex.StackTrace}");
        //        throw; // Kast exception videre eller håndter den på anden vis
        //    }
        //}






        public double ConvertDateTimeToDouble(string time)
        {
            DateTime parsedDateTime = DateTime.Parse(time);
            return double.Parse(parsedDateTime.ToString("yyyyMMddHHmm"));
        }



        /// <summary>
        /// Converts a given date and time to a float representation for internal use.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>a double value representing the date and time in float format. Alternatively, returns 0 on error.</returns>
        public double ConvertDateTimeToDouble_old(string date)
        {
            try
            {
                DateOnly parsedDate = DateOnly.Parse(date);
                DateTime parsedDateTime = parsedDate.ToDateTime(new TimeOnly(0, 0));

                string formattedDateTime = parsedDateTime.ToString("yyyyMMddHHmm");
                Debug.WriteLine($"Formatted DateTime: {formattedDateTime}");
                //var result = double.Parse(formattedDateTime);
                var result = 2;
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing date: {ex.Message}. Stack Trace: {ex.StackTrace}");
                return 0; // Return default value on error
            }
        }





        /// <summary>
        /// Splits a double representation of a date and time into separate components.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>an object array containing the date and time components.</returns>
        public object[] MixedYearDateTimeSplitter(double time)
        {
            string timeString = time.ToString("000000000000");
            object[] result = new object[2];

            try
            {
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
        /// Begins measuring the time and process of the current process,
        /// returns the initial CPU time and a stopwatch object.
        /// This method has no Unit Test because it uses the Process class, which is difficult to mock.
        /// It should rather be tested using integration tests.
        /// </summary>
        /// <returns>A tuple containing the initial CPU time and a stopwatch object.</returns>
        public (TimeSpan InitialCpuTime, Stopwatch Stopwatch) BeginMeasureCPU()
        {
            Process currentProcess = Process.GetCurrentProcess();
            TimeSpan initialCpuTime = currentProcess.TotalProcessorTime;
            Stopwatch stopwatch = Stopwatch.StartNew();

            return (initialCpuTime, stopwatch);
        }




        /// <summary>
        /// Stops measuring CPU usage, takes in a TimeSpan and a Stopwatch object,
        /// returns a tuple containing the CPU usage percentage and elapsed time in milliseconds.
        /// This method has no Unit Test because it uses the Process class, which is difficult to mock.
        /// It should rather be tested using integration tests.
        /// </summary>
        /// <param name="initialCpuTime"></param>
        /// <param name="stopwatch"></param>
        /// <returns>A tuple containing the CPU usage percentage and elapsed time in milliseconds.</returns>
        public (float CpuUsagePercentage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan initialCpuTime, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            Process currentProcess = Process.GetCurrentProcess();
            TimeSpan finalCpuTime = currentProcess.TotalProcessorTime;

            // Calculate CPU usage in terms of percentage
            double cpuUsedMs = (finalCpuTime - initialCpuTime).TotalMilliseconds;
            double elapsedTimeMs = elapsedTime.TotalMilliseconds;
            double cpuUsagePercentage = (cpuUsedMs / elapsedTimeMs) * 100;

            return (CpuUsagePercentage: (float)cpuUsagePercentage, ElapsedTimeMs: (float)elapsedTimeMs);
        }




        /// <summary>
        /// begins measuring ram usage, returns a 64 bit int representing the ram usage before the code block
        /// This method has no Unit Test because it uses the Process class, which is difficult to mock.
        /// It should rather be tested using integration tests.
        /// </summary>
        /// <returns>a 64-bit integer representing the RAM usage before executing the code block.</returns>
        public long BeginMeasureMemory()
        {
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            Process process = Process.GetCurrentProcess();
            long ramUsageBeforeBytes = process.PrivateMemorySize64;

            return ramUsageBeforeBytes;
        }




        /// <summary>
        /// stops measuring ram usage, takes in a 64 bit int representing the ram usage before the code block, returns the difference in ram usage
        /// This method has no Unit Test because it uses the Process class, which is difficult to mock.
        /// It should rather be tested using integration tests.
        /// </summary>
        /// <param name="ramUsageBeforeBytes"></param>
        /// <param name="currentProcess"></param>
        /// <returns>a 32-bit integer representing the difference in RAM usage after executing the code block.</returns>
        public long StopMeasureMemory(long ramUsageBeforeBytes)
        {
            long ramUsageAfterBytes = Process.GetCurrentProcess().PrivateMemorySize64;
            return ramUsageAfterBytes - ramUsageBeforeBytes;
        }




    }
}
