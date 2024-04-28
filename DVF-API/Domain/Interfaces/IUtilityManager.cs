using System.Diagnostics;
using System.Globalization;

namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        void CleanUpRessources();
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        int GetModelSize<T>(List<T> list);
        float ConvertBytesToMegabytes(int bytes);
        float ConvertBytesToGigabytes(int bytes);
        (TimeSpan, Stopwatch) BeginMeasureCPU();
        (float CpuUsage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan cpuTimeBefore, Stopwatch stopwatch);
        long BeginMeasureMemory();
        int StopMeasureMemory(long ramUsageBeforeBytes);
        string ConvertBytesToFormat(int bytes);
        double ConvertDateTimeToFloatInternal(string time);
        float ConvertCoordinate(string coordinate);
        object[] MixedYearDateTimeSplitter(double time);










    }
}
