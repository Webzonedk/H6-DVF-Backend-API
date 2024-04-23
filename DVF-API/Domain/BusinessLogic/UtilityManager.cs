using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.Services.ServiceImplementation;
using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

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


        public UtilityManager()
        {
        }





        /// <summary>
        /// Simple password-based authentication method that checks if the provided password matches the predefined secret password.
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
            var maxCoreCount = Environment.ProcessorCount-2;
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




    }
}
