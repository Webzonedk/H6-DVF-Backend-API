namespace DVF_API.Services.Interfaces
{
    internal interface IDeveloperService
    {
        void CreateHistoricWeatherDataAsync(bool createFiles, bool createDB);
        void StartSimulator();
        void StopSimulator();
    }
}
