using System.Diagnostics;

namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        float ConvertBytesToMegabytes(int bytes);
        float ConvertBytesToGigabytes(int bytes);
        (TimeSpan InitialCpuTime, Stopwatch Stopwatch) BeginMeasureCPU();
        (float CpuUsagePercentage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan initialCpuTime, Stopwatch stopwatch);
        long BeginMeasureMemory();
        long StopMeasureMemory(long ramUsageBeforeBytes);
        string ConvertTimeMeasurementToFormat(float time);
        string ConvertBytesToFormat(long bytes);
        double ConvertDateTimeToFloatInternal(string time);
        float ConvertCoordinate(string coordinate);
        object[] MixedYearDateTimeSplitter(double time);
    }
}
