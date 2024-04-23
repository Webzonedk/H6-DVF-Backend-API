using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.ServiceImplementation
{
    public class DataService : IDataService
    {

        private readonly IDatabaseRepository _databaseRepository;
        private readonly ILocationRepository _locationRepository;

        public DataService(IDatabaseRepository databaseRepository, ILocationRepository locationRepository)
        {
            _databaseRepository = databaseRepository;
            _locationRepository = locationRepository;
        }
        public async Task<List<string>> GetAddressesFromDBMatchingInputs(string partialAddress)
        {
            return await _locationRepository.FetchMatchingAddresses(partialAddress);
        }

        public Task<int> CountLocations()
        {
            return _locationRepository.FetchLocationCount();
        }

        public async Task<List<string>> GetLocationCoordinates(int fromIndex, int toIndex)
        {
            return await _locationRepository.FetchLocationCoordinates(fromIndex, toIndex);
        }

        public async Task<MetaDataDto> GetWeatherDataService(SearchDto searchDto)
        {
            return await _databaseRepository.FetchWeatherData(searchDto);
        }
    }
}
