﻿using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface ICrudFileRepository
    {
        Task<List<BinaryDataFromFileDto>> FetchWeatherDataAsync(SearchDto search);
        void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate);
        void RestoreAllData();
        void InsertData(WeatherDataFromIOTDto weatherDataFromIOT);
    }
}
