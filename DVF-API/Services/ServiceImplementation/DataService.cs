using DVF_API.Data.Interfaces;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.ServiceImplementation
{
    public class DataService : IDataService
    {

        private readonly ICrudDatabaseRepository _crudDatabaseRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICrudFileRepository _crudFileRepository;
        private readonly IBinaryConversionManager _binaryConversionManager;

        public DataService(ICrudDatabaseRepository crudDatabaseRepository, ILocationRepository locationRepository, ICrudFileRepository crudFileRepository, IBinaryConversionManager binaryConversionManager)
        {
            _crudDatabaseRepository = crudDatabaseRepository;
            _locationRepository = locationRepository;
            _crudFileRepository = crudFileRepository;
            _binaryConversionManager = binaryConversionManager;

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
            if (searchDto.ToggleDB)
            {
                return await _crudDatabaseRepository.FetchWeatherDataAsync(searchDto);
            }
            if (!searchDto.ToggleDB)
            {
                List<WeatherDataFileDto> weatherDataFileDtoList = new List<WeatherDataFileDto>();
                List<byte[]> listOfByteArrays = await _crudFileRepository.FetchWeatherDataAsync(searchDto);

                for (int i = 0; i < listOfByteArrays.Count; i++)
                {
                    WeatherDataFileDto weatherDataFileDto = _binaryConversionManager.ConvertBinaryDataToWeatherDataFileDto(listOfByteArrays[i]);
                    weatherDataFileDtoList.Add(weatherDataFileDto);
                }
                return null;
            }
                return new MetaDataDto();
        }
    }
}
