using DVF_API.Data.Interfaces;
using DVF_API.Data.Mappers;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Services.ServiceImplementation
{
    public class DataService: IDataService
    {

        private readonly IDataRepository _dataRepository;

        public DataService(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }
        public List<string> GetAddressesFromDBMatchingInputs(string partialAddress)
        {
            return new List<string>();
        }

        public int CountLocations()
        {
            return 0;
        }

        public List<string> GetLocationCoordinates(int fromIndex, int toIndex)
        {
            return new List<string>();
        }

        public MetaDataDto GetWeatherDataService(SearchDto searchDto)
        {

            return _dataRepository.FetchWeatherData(searchDto);
        }
    }
}
