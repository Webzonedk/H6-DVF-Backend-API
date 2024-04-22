﻿using DVF_API.Data.Models;

namespace DVF_API.Services.Interfaces
{
    public interface IDeveloperService
    {
        void CreateHistoricWeatherDataAsync(bool createFiles, bool createDB);
        void StartSimulator();
        void StopSimulator();
        void CreateCities();
        void CreateLocations();
    }
}
