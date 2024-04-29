﻿using DVF_API.SharedLib.Dtos;

namespace DVF_API.Data.Interfaces
{
    public interface ILocationRepository
    {
        Task<List<string>> FetchMatchingAddresses(string partialAddress);
        Task<int> FetchLocationCount();
        Task<Dictionary<long, string>> FetchLocationCoordinates(int fromIndex, int toIndex);
        Task<List<BinaryDataFromFileDto>> FetchAddressByCoordinates(SearchDto searchDto);
        Task<Dictionary<long, LocationDto>> GetAllLocationCoordinates();
    }
}
