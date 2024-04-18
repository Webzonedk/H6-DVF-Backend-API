using System.Diagnostics;

namespace DVF_API.Domain.BusinessLogic
{
    /// <summary>
    /// Provides methods for performing system maintenance tasks such as freeing up system resources,
    /// clearing the console, and terminating redundant processes associated with the current application.
    /// This class is designed to ensure that the application environment is clean and that resources
    /// are managed efficiently, especially before shutdown or when resource reallocation is necessary.
    /// </summary>
    public class MaintenanceManager
    {
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

            Console.Clear();

            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    process.Kill();
                }
            }
        }

    }
}
