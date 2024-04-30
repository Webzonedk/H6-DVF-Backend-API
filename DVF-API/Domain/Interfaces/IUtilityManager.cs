using System.Diagnostics;

namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        string ConvertTimeMeasurementToFormat(float time);
        string ConvertBytesToFormat(long bytes);
        double ConvertDateTimeToDouble(string time);
        object[] MixedYearDateTimeSplitter(double time);
        (TimeSpan InitialCpuTime, Stopwatch Stopwatch) BeginMeasureCPU();
        (float CpuUsagePercentage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan initialCpuTime, Stopwatch stopwatch);
        long BeginMeasureMemory();
        long StopMeasureMemory(long ramUsageBeforeBytes);
    }
}
