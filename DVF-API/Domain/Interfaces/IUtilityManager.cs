using System.Diagnostics;

namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        //string ConvertTimeMeasurementToFormat(float time);
        string ConvertBytesToFormat(double bytes);
        long ConvertDateTimeToDouble(string time);
        object[] MixedYearDateTimeSplitter(double time);
        //(TimeSpan InitialCpuTime, Stopwatch Stopwatch) BeginMeasureCPUTime();
        //(float CpuUsagePercentage, float ElapsedTimeMs) StopMeasureCPUTime(TimeSpan initialCpuTime, Stopwatch stopwatch);
        //long BeginMeasureMemory();
        //long StopMeasureMemory(long startMemory);
        (TimeSpan, Stopwatch) BeginMeasureCPUTime();
        (double CpuUsagePercentage, double ElapsedTimeMs) StopMeasureCPUTime(TimeSpan startTime, Stopwatch stopwatch);
        double BeginMeasureMemory();
        double StopMeasureMemory(double startMemory);
        string ConvertTimeMeasurementToFormat(double timeMs);
    }
}
