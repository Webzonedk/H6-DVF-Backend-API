using System.Diagnostics;

namespace DVF_API.Domain.Interfaces
{
    /// <summary>
    /// This interface connects the utility manager with the utility service
    /// </summary>
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        string ConvertBytesToFormat(double bytes);
        long ConvertDateTimeToDouble(string time);
        object[] MixedYearDateTimeSplitter(double time);
        (TimeSpan, Stopwatch) BeginMeasureCPUTime();
        (double CpuUsagePercentage, double ElapsedTimeMs) StopMeasureCPUTime(TimeSpan startTime, Stopwatch stopwatch);
        double BeginMeasureMemory();
        double StopMeasureMemory(double startMemory);
        string ConvertTimeMeasurementToFormat(double timeMs);
    }
}
