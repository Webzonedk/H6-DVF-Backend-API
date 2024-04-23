using DVF_API.Data.Models;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherDataController : ControllerBase
    {

        #region fields
        private readonly IDataService _dataService;
        #endregion

        #region Constructor
        public WeatherDataController(IDataService dataService)
        {
            _dataService = dataService;
        }
        #endregion

        [HttpPost("/GetAddress")]
        public async Task < IEnumerable<string>> GetAddress(string addressInput)
        {
            return await _dataService.GetAddressesFromDBMatchingInputs(addressInput);
        }

        [HttpGet("/GetLocationCount")]
        public Task< int> GetLocationCount()
        {
          return _dataService.CountLocations();
        }


        [HttpPost("/GetLocations")]
        public async Task< IEnumerable<string>> GetLocations(int fromIndex, int toIndex)
        {
            return await _dataService.GetLocationCoordinates(fromIndex, toIndex);
        }

        [HttpPost("/GetWeatherData")]
        public async Task< MetaDataDto> GetWeatherData(SearchDto searchDto)
        {
           return await _dataService.GetWeatherDataService(searchDto);
        }
    }
}
