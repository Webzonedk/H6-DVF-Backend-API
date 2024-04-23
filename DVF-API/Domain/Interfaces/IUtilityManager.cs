using System.Diagnostics;

namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        void CleanUpRessources();
        int CalculateOptimalDegreeOfParallelism();
        int GetModelSize(object obj);
        int GetModelSize<T>(List<T> list);
        float ConvertBytesToMegabytes(int bytes);
        float ConvertBytesToGigabytes(int bytes);
        (TimeSpan, Stopwatch) BeginMeasureCPU();
        (float CpuUsage, float ElapsedTimeMs) StopMeasureCPU(TimeSpan cpuTimeBefore, Stopwatch stopwatch);
        (Process currentProcess, long processBytes) BeginMeasureMemory();
        long StopMeasureMemory(long ramUsageBeforeBytes, Process currentProcess);


    }
}
